import {
  Namespace,
  Program,
} from "@typespec/compiler";
import {
  Node,
  AliasStatementNode,
  ModelStatementNode,
  OperationStatementNode,
  InterfaceStatementNode,
  EnumStatementNode,
  NamespaceStatementNode,
  ModelExpressionNode,
  IntersectionExpressionNode,
  SyntaxKind,
  BaseNode,
  IdentifierNode,
  ModelPropertyNode,
  EnumMemberNode,
  ModelSpreadPropertyNode,
  EnumSpreadMemberNode,
  DecoratorExpressionNode,
  MemberExpressionNode,
  UnionStatementNode,
  UnionExpressionNode,
  UnionVariantNode,
  AugmentDecoratorStatementNode,
  ScalarStatementNode,
  TypeReferenceNode,
  JsNamespaceDeclarationNode,
  DirectiveExpressionNode,
  StringLiteralNode,
  ObjectLiteralNode,
  ConstStatementNode,
  visitChildren
} from "@typespec/compiler/ast";
export class NamespaceModel {
  kind = SyntaxKind.NamespaceStatement;
  name: string;
  node: NamespaceStatementNode | JsNamespaceDeclarationNode;
  operations = new Map<string, OperationStatementNode | InterfaceStatementNode>();
  resources = new Map<
    string,
    | AliasStatementNode
    | ModelStatementNode
    | ModelExpressionNode
    | IntersectionExpressionNode
    | EnumStatementNode
    | ScalarStatementNode
    | UnionStatementNode
    | UnionExpressionNode
    | ObjectLiteralNode
  >();
  models = new Map<
    string,
    | ModelStatementNode
    | ModelExpressionNode
    | IntersectionExpressionNode
    | EnumStatementNode
    | ScalarStatementNode
    | UnionStatementNode
    | UnionExpressionNode
    | ObjectLiteralNode
  >();
  aliases = new Map<string, AliasStatementNode>();
  augmentDecorators = new Array<AugmentDecoratorStatementNode>();
  constants = new Array<ConstStatementNode>();

  constructor(name: string, ns: Namespace, program: Program) {
    this.name = name;
    this.node = ns.node!; // Assuming ns.node is never undefined

    // Gather operations
    for (const [opName, op] of ns.operations) {
      if (op.node) {
        this.operations.set(opName, op.node);
      } else {
        throw new Error(`Operation node for ${opName} is undefined.`);
      }
    }
    for (const [intName, int] of ns.interfaces) {
      if (int.node) {
        this.operations.set(intName, int.node);
      } else {
        throw new Error(`Interface node for ${intName} is undefined.`);
      }
    }

    // Gather models and resources
    for (const [modelName, model] of ns.models) {
      if (model.node !== undefined) {
        let isResource = false;
        for (const dec of model.decorators) {
          if (dec.decorator.name === "$resource") {
            isResource = true;
            break;
          }
        }
        if (isResource) {
          this.resources.set(modelName, model.node);
        } else {
          this.models.set(modelName, model.node);
        }
      } else {
        throw new Error("Unexpectedly found undefined model node.");
      }
    }
    for (const [enumName, en] of ns.enums) {
      if (en.node) {
        this.models.set(enumName, en.node);
      } else {
        throw new Error(`Enum node for ${enumName} is undefined.`);
      }
    }
    for (const [unionName, un] of ns.unions) {
      if (un.node) {
        this.models.set(unionName, un.node);
      } else {
        throw new Error(`Union node for ${unionName} is undefined.`);
      }
    }
    for (const [scalarName, sc] of ns.scalars) {
      if (sc.node) {
        this.models.set(scalarName, sc.node);
      } else {
        throw new Error(`Scalar node for ${scalarName} is undefined.`);
      }
    }

    // Gather aliases
    for (const alias of findNodes(SyntaxKind.AliasStatement, program, ns)) {
      this.aliases.set(alias.id.sv, alias);
    }

    // collect augment decorators
    for (const augment of findNodes(SyntaxKind.AugmentDecoratorStatement, program, ns)) {
      this.augmentDecorators.push(augment);
    }

    // collect constants
    for (const constant of findNodes(SyntaxKind.ConstStatement, program, ns)) {
      this.constants.push(constant);
    }

    // sort operations and models
    this.operations = new Map([...this.operations].sort(caseInsensitiveSort));
    this.resources = new Map([...this.resources].sort(caseInsensitiveSort));
    this.models = new Map([...this.models].sort(caseInsensitiveSort));
    this.aliases = new Map([...this.aliases].sort(caseInsensitiveSort));
  }

  /**
   * Don't emit an empty namespace
   * @returns true if there are models, resources or operations
   */
  shouldEmit(): boolean {
    return (
      (this.node as NamespaceStatementNode).decorators !== undefined ||
      this.models.size > 0 ||
      this.operations.size > 0 ||
      this.resources.size > 0
    );
  }
}

function findNodes<T extends SyntaxKind>(kind: T, program: Program, namespace: Namespace): (Node & { kind: T })[] {
  const nodes: Node[] = [];
  for (const file of program.sourceFiles.values()) {
    visitChildren(file, function visit(node) {
      if (node.kind === kind && inNamespace(node, program, namespace)) {
        nodes.push(node);
      }
      visitChildren(node, visit);
    });
  }
  return nodes as any;
}

function inNamespace(node: Node, program: Program, namespace: Namespace): boolean {
  for (let n: Node | undefined = node; n; n = n.parent) {
    switch (n.kind) {
      case SyntaxKind.NamespaceStatement:
        return program.checker.getTypeForNode(n) === namespace;
      case SyntaxKind.TypeSpecScript:
        if (n.inScopeNamespaces.length > 0 && inNamespace(n.inScopeNamespaces[0], program, namespace)) {
          return true;
        }
        return false;
    }
  }
  return false;
}

export function generateId(obj: BaseNode | NamespaceModel | undefined): string | undefined {
  let node;
  if (obj === undefined) {
    return undefined;
  }
  if (obj instanceof NamespaceModel) {
    return obj.name;
  }
  let name: string;
  let parentId: string | undefined;
  switch (obj.kind) {
    case SyntaxKind.NamespaceStatement:
      node = obj as NamespaceStatementNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.DecoratorExpression:
      node = obj as DecoratorExpressionNode;
      switch (node.target.kind) {
        case SyntaxKind.Identifier:
          return `@${node.target.sv}`;
        case SyntaxKind.MemberExpression:
          return generateId(node.target);
      }
    case SyntaxKind.EnumMember:
      node = obj as EnumMemberNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.EnumSpreadMember:
      node = obj as EnumSpreadMemberNode;
      return generateId(node.target);
    case SyntaxKind.EnumStatement:
      node = obj as EnumStatementNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.Identifier:
      node = obj as IdentifierNode;
      return node.sv;
    case SyntaxKind.InterfaceStatement:
      node = obj as InterfaceStatementNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.MemberExpression:
      node = obj as MemberExpressionNode;
      name = node.id.sv;
      parentId = generateId(node.base);
      break;
    case SyntaxKind.ModelProperty:
      node = obj as ModelPropertyNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.ModelSpreadProperty:
      node = obj as ModelSpreadPropertyNode;
      name = generateId(node.target)!;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.ModelStatement:
      node = obj as ModelStatementNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.OperationStatement:
      node = obj as OperationStatementNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.StringLiteral:
      node = obj as StringLiteralNode;
      name = node.value;
      parentId = undefined;
      break;
    case SyntaxKind.UnionStatement:
      node = obj as UnionStatementNode;
      name = node.id.sv;
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.UnionVariant:
      node = obj as UnionVariantNode;
      if (node.id?.sv !== undefined) {
        name = node.id.sv;
      } else {
        // TODO: Should never have default value of _
        name = generateId(node.value) ?? "_";
      }
      parentId = generateId(node.parent);
      break;
    case SyntaxKind.TypeReference:
      node = obj as TypeReferenceNode;
      name = generateId(node.target)!;
      parentId = undefined;
      break;
    case SyntaxKind.DirectiveExpression:
      node = obj as DirectiveExpressionNode;
      name = `#${generateId(node.target)!}`;
      for (const arg of node.arguments) {
        name += `_${generateId(arg)}`;
      }
      parentId = generateId(node.parent);
      break;
    default:
      return undefined;
  }
  if (parentId !== undefined) {
    return `${parentId}.${name}`;
  } else {
    return name;
  }
}

function caseInsensitiveSort(a: [string, any], b: [string, any]): number {
  const aLower = a[0].toLowerCase();
  const bLower = b[0].toLowerCase();
  return aLower > bLower ? 1 : aLower < bLower ? -1 : 0;
}
