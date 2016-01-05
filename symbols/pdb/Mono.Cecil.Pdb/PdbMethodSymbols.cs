using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb {
    public sealed class PdbMethodSymbols : MethodSymbols {
        private List<string> used_namespaces;
        private List<ushort> using_counts;

        public PdbMethodSymbols (MethodBody methodBody) : base (methodBody)
        {
        }

        public string IteratorClass { get; set; }

        public List<PdbIteratorScope> IteratorScopes { get; set; }

        public List<string> UsedNamespaces { get; set; }

        public List<ushort> UsingCounts { get; set; }

        public MethodReference MethodWhoseUsingInfoAppliesToThisMethod { get; set; }

        public PdbSynchronizationInformation SynchronizationInformation { get; set; }
    }

    public class PdbSynchronizationInformation {
        public MethodReference KickoffMethod { get; set; }
        public uint GeneratedCatchHandlerIlOffset { get; set; }
        public List<PdbSynchronizationPoint> SynchronizationPoints { get; set; }
    }

    public class PdbSynchronizationPoint {
        internal uint SynchronizeOffset { get; set; }
        internal MethodReference ContinuationMethod { get; set; }
        internal uint ContinuationOffset { get; set; }
    }
}