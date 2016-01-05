using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb {
    public class PdbIteratorScope {
        public Instruction Start { get; private set; }
        public Instruction End { get; private set; }

        public PdbIteratorScope (Instruction start, Instruction end)
        {
            Start = start;
            End = end;
        }
    }
}