using System.Collections.Generic;

namespace Azure.Sdk.Tools.TypeSpecValidator.Rules.Swagger
{
    internal interface SwaggerRule
    {
        RuleResult Evaluate(SwaggerFile swaggerFile, IEnumerable<TypeSpecProject> typeSpecProjects);
    }
}
