using System.Collections.Generic;

namespace Azure.Sdk.Tools.TypeSpecValidator.Rules.Swagger
{
    internal abstract class SwaggerRule
    {
        public abstract RuleResult Evaluate(SwaggerFile swaggerFile, IEnumerable<TypeSpecProject> typeSpecProjects);
    }
}
