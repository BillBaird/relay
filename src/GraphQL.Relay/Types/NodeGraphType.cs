using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EventStore.Common.Persistence;
using GraphQL.Types;
using GraphQL.Types.Relay;
using Panic.StringUtils;
using SBLabs.Protocols.EventStore;
using SBLabs.Protocols.EventStore.GraphQL;
using SBLabs.Protocols.Utils;

namespace GraphQL.Relay.Types
{
    public class GlobalId {
        public string Type, Id;
    }

    public interface IRelayNode<out T>
    {
        T GetById(string id);
    }

    public static class Node
    {
        public static NodeGraphType<TSource, TOut> For<TSource, TOut>(Func<string, TOut> getById)
        {
            var type = new DefaultNodeGraphType<TSource, TOut>(getById);
            return type;
        }

        public static string ToGlobalId(string name, object id, IdScope scope)
        {
            return scope == IdScope.Global 
                ? "t:{0}:{1}".ToFormat(name, id).BookmarkEncode() 
                : id.ToString();
        }

        public static GlobalId FromGlobalId(string globalId)
        {
            var parts = globalId.BookmarkDecodeToString().Split(':');
            if (parts.Length != 3)
                throw new ArgumentException($"String Id value ({globalId}) is not a valid Global Id");
            return new GlobalId {
                Type = parts[1],
                Id = string.Join(":", parts.Skip(count: 2)),
            };
        }
    }


    public abstract class NodeGraphType<T, TOut> : ObjectGraphType<T>, IRelayNode<TOut>
    {
        public static Type Edge => typeof(EdgeType<NodeGraphType<T, TOut>>);

        public static Type Connection => typeof(ConnectionType<NodeGraphType<T, TOut>>);

        protected NodeGraphType()
        {
            Interface<NodeInterface>();
        }

        public abstract TOut GetById(string id);

        public FieldType Id<TReturnType>(Expression<Func<T, TReturnType>> expression)
        {
            string name = null;
            try
            {
                name = StringUtils.ToCamelCase(expression.NameOf());
            }
            catch
            {
            }


            return Id(name, expression);
        }

        public FieldType Id<TReturnType>(string name, Expression<Func<T, TReturnType>> expression)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                // if there is a field called "ID" on the object, namespace it to "contactId"
                if (name.ToLower() == "id")
                {
                    if (string.IsNullOrWhiteSpace(Name))
                        throw new InvalidOperationException(
                            "The parent GraphQL type must define a Name before declaring the Id field " +
                            "in order to properly prefix the local id field");

                    name = StringUtils.ToCamelCase(Name + "Id");
                }

                Field<NonNullGraphType<StringGraphType>>(
                    name,
                    description: $"The Id of the {Name ?? "node"}",
                    arguments: new QueryArguments(
                        new QueryArgument<IdFormatEnum>{Name = "format", Description = @"The optional format the Id should be returned in - UUID (the default), ORDINAL, HASH_ORDINAL, or HASH_64_ORDINAL", DefaultValue = IdFormat.UUID},
                        new QueryArgument<IdTypeFormatEnum>{Name = "typeFormat", Description = @"Specifies if the reference should be type qualified, and if so in what form - TABLE_NAME, TYPE_NAME, or NONE (the default).", DefaultValue = IdTypeFormat.None}
                    ),
                    resolve: context =>
                    {
                        var format = context.GetArgument<IdFormat?>("format").Default<IdFormat>(IdFormat.UUID);
                        var typeFormat = context.GetArgument<IdTypeFormat?>("typeFormat").Default<IdTypeFormat>(IdTypeFormat.None);
                        return _IdRef.MakeIdStr(((IProvideContext)context.Source)._context, format, typeFormat);
                    }) // VALUETYPE
                    .Metadata["RelayLocalIdField"] = true;
            }

            var idArg = new QueryArguments(
                new QueryArgument<IdScopeEnum>{Name = "scope", Description = @"The optional scope the Id should be returned in - GLOBAL (the default which is suitable for the node query) or LOCAL", DefaultValue = IdScope.Global}
            );
            
            var field = Field(
                name: "id",
                description: $"The Id of the {Name ?? "node"}, either in GLOBAL scope (the default) or as LOCAL scope.",
                arguments: idArg,
                type: typeof(NonNullGraphType<IdGraphType>),
                resolve: context => Node.ToGlobalId(
                    context.ParentType.Name,
                    expression.Compile()(context.Source),
                    context.GetArgument<IdScope?>("scope").Default<IdScope>(IdScope.Global)
                )
            );

            field.Metadata["RelayGlobalIdField"] = true;

            if (!string.IsNullOrWhiteSpace(name))
                field.Metadata["RelayRelatedLocalIdField"] = name;

            return field;
        }
    }

    public abstract class NodeGraphType<TSource> : NodeGraphType<TSource, TSource>
    {
    }

    public abstract class NodeGraphType : NodeGraphType<object>
    {
    }

    public abstract class AsyncNodeGraphType<T> : NodeGraphType<T, Task<T>>
    {
    }

    public class DefaultNodeGraphType<TSource, TOut> : NodeGraphType<TSource, TOut>
    {
        private readonly Func<string, TOut> _getById;

        public DefaultNodeGraphType(Func<string, TOut> getById)
        {
            _getById = getById;
        }

        public override TOut GetById(string id)
        {
            return _getById(id);
        }
    }
}
