//
// PdbReader.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Cci.Pdb;

using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb {

	public class PdbReader : ISymbolReader {

		int age;
		Guid guid;

		readonly Stream pdb_file;
		readonly Dictionary<string, Document> documents = new Dictionary<string, Document> ();
		readonly Dictionary<uint, PdbFunction> functions = new Dictionary<uint, PdbFunction> ();

		internal PdbReader (Stream file)
		{
			this.pdb_file = file;
		}

		/*
		uint Magic = 0x53445352;
		Guid Signature;
		uint Age;
		string FileName;
		 */

		public bool ProcessDebugHeader (ImageDebugDirectory directory, byte [] header)
		{
			if (header.Length < 24)
				return false;

			var magic = ReadInt32 (header, 0);
			if (magic != 0x53445352)
				return false;

			var guid_bytes = new byte [16];
			Buffer.BlockCopy (header, 4, guid_bytes, 0, 16);

			this.guid = new Guid (guid_bytes);
			this.age = ReadInt32 (header, 20);

			return PopulateFunctions ();
		}

		static int ReadInt32 (byte [] bytes, int start)
		{
			return (bytes [start]
				| (bytes [start + 1] << 8)
				| (bytes [start + 2] << 16)
				| (bytes [start + 3] << 24));
		}

		bool PopulateFunctions ()
		{
			using (pdb_file) {
				Dictionary<uint, PdbTokenLine> tokenToSourceMapping;
				string sourceServerData;
				int age;
				Guid guid;

				var funcs = PdbFile.LoadFunctions (pdb_file, out tokenToSourceMapping,  out sourceServerData, out age, out guid);

				if (this.guid != guid)
					return false;

				foreach (PdbFunction function in funcs)
					functions.Add (function.token, function);
			}

			return true;
		}

		public void Read (MethodBody body, InstructionMapper mapper)
		{
			var method_token = body.Method.MetadataToken;

			PdbFunction function;
			if (!functions.TryGetValue (method_token.ToUInt32 (), out function))
				return;

			ReadSequencePoints (function, mapper);
			ReadScopeAndLocals (function.scopes, null, body, mapper);

			if (!string.IsNullOrEmpty (function.iteratorClass))
				DebugInformationFor (body).IteratorType = body.Method.DeclaringType.GetNestedType (function.iteratorClass);

			if (function.iteratorScopes == null)
				return;

			foreach (var scope in function.iteratorScopes) {
				var range = new InstructionRange ();
				PopulateInstructionRange (range, scope.Offset, scope.Length, body, mapper);
				DebugInformationFor (body).IteratorScopes.Add (range);
			}
		}

		static MethodDebugInformation DebugInformationFor (MethodBody body)
		{
			return body.debug_information ?? (body.debug_information = new MethodDebugInformation ());
		}

		static void PopulateInstructionRange (InstructionRange range, uint offset, uint length, MethodBody body, InstructionMapper mapper)
		{
			var next = mapper ((int) (offset + length));

			range.Start = mapper ((int) offset);
			range.End = next != null
				? next.Previous
				: body.Instructions [body.Instructions.Count - 1];
		}

		static void ReadScopeAndLocals (PdbScope [] scopes, Scope parent, MethodBody body, InstructionMapper mapper)
		{
			foreach (PdbScope scope in scopes)
				ReadScopeAndLocals (scope, parent, body, mapper);

			CreateRootScope (body);
		}

		static void CreateRootScope (MethodBody body)
		{
			if (!body.HasVariables)
				return;

			var instructions = body.Instructions;

			var root = new Scope {
				Start = instructions [0],
				End = instructions [instructions.Count - 1]
			};

			var variables = body.Variables;
			for (int i = 0; i < variables.Count; i++)
				root.Variables.Add (variables [i]);

			body.DebugInformation.Scope = root;
		}

		static void ReadScopeAndLocals (PdbScope pdbScope, Scope parent, MethodBody body, InstructionMapper mapper)
		{
			if (pdbScope == null)
				return;

			var scope = new Scope ();
			PopulateInstructionRange (scope, pdbScope.offset, pdbScope.length, body, mapper);

			if (parent != null)
				parent.Scopes.Add (scope);
			else if (DebugInformationFor (body).Scope == null)
				DebugInformationFor (body).Scope = scope;
			else
				throw new NotSupportedException () ;

			foreach (var slot in pdbScope.slots) {
				int index = (int) slot.slot;
				if (index < 0 || index >= body.Variables.Count)
					continue;

				var variable = body.Variables [index];
				variable.Name = slot.name;

				scope.Variables.Add (variable);
			}

			ReadScopeAndLocals (pdbScope.scopes, scope, body, mapper);
		}

		void ReadSequencePoints (PdbFunction function, InstructionMapper mapper)
		{
			if (function.lines == null)
				return;

			foreach (var lines in function.lines)
				ReadLines (lines, mapper);
		}

		void ReadLines (PdbLines lines, InstructionMapper mapper)
		{
			var document = GetDocument (lines.file);

			foreach (var line in lines.lines)
				ReadLine (line, document, mapper);
		}

		static void ReadLine (PdbLine line, Document document, InstructionMapper mapper)
		{
			var instruction = mapper ((int) line.offset);
			if (instruction == null)
				return;

			instruction.SequencePoint = new SequencePoint (document) {
				StartLine = (int) line.lineBegin,
				StartColumn = (int) line.colBegin,
				EndLine = (int) line.lineEnd,
				EndColumn = (int) line.colEnd
			};
		}

		Document GetDocument (PdbSource source)
		{
			string name = source.name;
			Document document;
			if (documents.TryGetValue (name, out document))
				return document;

			document = new Document (name) {
				Language = source.language.ToLanguage (),
				LanguageVendor = source.vendor.ToVendor (),
				Type = source.doctype.ToType (),
			};
			documents.Add (name, document);
			return document;
		}

		public void Read (MethodSymbols symbols)
		{
			PdbFunction function;
			if (!functions.TryGetValue (symbols.MethodToken.ToUInt32 (), out function))
				return;

			ReadSequencePoints (function, symbols);
			ReadLocals (function.scopes, symbols);

			if (!string.IsNullOrEmpty (function.iteratorClass))
				symbols.IteratorType = function.iteratorClass;

			if (function.iteratorScopes == null)
				return;

			foreach (var scope in function.iteratorScopes) {
				symbols.IteratorScopes.Add (new InstructionRangeSymbol {
					start = (int) scope.Offset,
					end = (int) (scope.Offset + scope.Length)
				});
			}
		}

		void ReadLocals (PdbScope [] scopes, MethodSymbols symbols)
		{
			foreach (var scope in scopes)
				ReadLocals (scope, symbols);
		}

		void ReadLocals (PdbScope scope, MethodSymbols symbols)
        {
            if (scope == null)
                return;

            foreach (var slot in scope.slots) {
                int index = (int) slot.slot;
                if (index < 0 || index >= symbols.Variables.Count)
                    continue;

                var variable = symbols.Variables [index];
                variable.Name = slot.name;
            }

            ReadLocals (scope.scopes, symbols);
        }

		void ReadSequencePoints (PdbFunction function, MethodSymbols symbols)
		{
			if (function.lines == null)
				return;

			foreach (var lines in function.lines)
				ReadLines (lines, symbols);
		}

		void ReadLines (PdbLines lines, MethodSymbols symbols)
		{
			for (int i = 0; i < lines.lines.Length; i++) {
				var line = lines.lines [i];

				symbols.Instructions.Add (new InstructionSymbol ((int) line.offset, new SequencePoint (GetDocument (lines.file)) {
					StartLine = (int) line.lineBegin,
					StartColumn = (int) line.colBegin,
					EndLine = (int) line.lineEnd,
					EndColumn = (int) line.colEnd,
				}));
			}
		}

		public void Dispose ()
		{
			pdb_file.Close ();
		}
	}
}
