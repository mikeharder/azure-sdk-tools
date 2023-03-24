using System.Collections.Generic;

namespace Azure.Sdk.Tools.TypeSpecValidator.Rules.TypeSpec
{
    internal abstract class TypeSpecRule
    {
        public abstract RuleResult Evaluate(TypeSpecProject typeSpecProject, IEnumerable<SwaggerFile> swaggerFiles);
    }
}
