namespace Mono.Cecil.Cil {
    sealed class SymbolReaderResolver : ISymbolReaderResolver {
        private MetadataBuilder metadata;
        private MetadataReader reader;

        public SymbolReaderResolver(MetadataBuilder metadata, MetadataReader reader)
        {
            this.metadata = metadata;
            this.reader = reader;
        }

        public MethodReference LookupMethod(MetadataToken old_token)
        {
            var provider = reader.LookupToken(old_token);
            return provider as MethodReference;
        }
    }
}