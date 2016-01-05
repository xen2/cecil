namespace Mono.Cecil.Cil {
    sealed class SymbolReaderResolver : ISymbolReaderResolver {
        private MetadataReader reader;

        public SymbolReaderResolver(MetadataReader reader)
        {
            this.reader = reader;
        }

        public MethodReference LookupMethod(MetadataToken old_token)
        {
            var provider = reader.LookupToken(old_token);
            return provider as MethodReference;
        }
    }
}