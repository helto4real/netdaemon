using JoySoftware.HomeAssistant.Client;
using JoySoftware.HomeAssistant.NetDaemon.Daemon.Config;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NetDaemon.Daemon.Tests")]

namespace JoySoftware.HomeAssistant.NetDaemon.DaemonRunner.Service.App
{
    public class CodeGenerator
    {
        /// <summary>
        ///     Mapps the domain to corresponding implemented Fluent API, will be added as
        ///     more and more entity types are supported
        /// </summary>
        private static IDictionary<string, (string, string)> _FluentApiMapper = new Dictionary<string, (string, string)>
        {
            ["light"] = ("Entity", "IEntity"),
            ["script"] = ("Entity", "IEntity"),
            ["scene"] = ("Entity", "IEntity"),
            ["switch"] = ("Entity", "IEntity"),
            ["camera"] = ("Camera", "ICamera"),
            ["media_player"] = ("MediaPlayer", "IMediaPlayer"),
            ["automation"] = ("Entity", "IEntity"),
            // ["input_boolean"],
            // ["remote"],
            // ["climate"],
        };

        public string? GenerateCode(string nameSpace, IEnumerable<string> entities)
        {
            var code = SyntaxFactory.CompilationUnit();

            // Add Usings statements
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("JoySoftware.HomeAssistant.NetDaemon.Common")));

            // Add namespace
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nameSpace)).NormalizeWhitespace();

            // Add support for extensions for entities
            var extensionClass = SyntaxFactory.ClassDeclaration("EntityExtension");

            extensionClass = extensionClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword));

            // Get all available domains, this is used to create the extensionmethods
            var domains = GetDomainsFromEntities(entities);

            foreach (var domain in domains)
            {
                if (_FluentApiMapper.ContainsKey(domain))
                {
                    var camelCaseDomain = domain.ToCamelCase();
                    var method = $@"public static {camelCaseDomain}Entities {camelCaseDomain}Ex(this NetDaemonApp app) => new {camelCaseDomain}Entities(app);";
                    var methodDeclaration = CSharpSyntaxTree.ParseText(method).GetRoot().ChildNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    extensionClass = extensionClass.AddMembers(methodDeclaration);
                }
            }
            namespaceDeclaration = namespaceDeclaration.AddMembers(extensionClass);

            // Add the classes implementing the specific entities
            foreach (var domain in GetDomainsFromEntities(entities))
            {
                
                if (_FluentApiMapper.ContainsKey(domain))
                {
                    var classDeclaration = $@"public partial class {domain.ToCamelCase()}Entities
    {{
        private readonly NetDaemonApp _app;

        public {domain.ToCamelCase()}Entities(NetDaemonApp app)
        {{
            _app = app;
        }}
    }}";
                    var entityClass = CSharpSyntaxTree.ParseText(classDeclaration).GetRoot().ChildNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                    foreach (var entity in entities.Where(n => n.StartsWith(domain)))
                    {
                        var (fluent, fluentInterface) = _FluentApiMapper[domain];
                        var name = entity[(entity.IndexOf(".") + 1)..];
                        // Quick check to make sure the name is a valid C# identifier. Should really check to make
                        // sure it doesn't collide with a reserved keyword as well.
                        if (!char.IsLetter(name[0]) && (name[0] != '_'))
                        {
                            name = "e_" + name;
                        }

                        var propertyCode = $@"public {fluentInterface} {name.ToCamelCase()} => _app.{fluent}(""{entity}"");";
                        var propDeclaration = CSharpSyntaxTree.ParseText(propertyCode).GetRoot().ChildNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                        entityClass = entityClass.AddMembers(propDeclaration);
                    }
                    namespaceDeclaration = namespaceDeclaration.AddMembers(entityClass);
                }
            }

            code = code.AddMembers(namespaceDeclaration);

            return code.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString();
        }

        public string? GenerateCodeRx(string nameSpace, IEnumerable<string> entities, IEnumerable<HassServiceDomain> services)
        {
            var code = SyntaxFactory.CompilationUnit();

            // Add Usings statements
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")));
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")));
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Dynamic")));
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("JoySoftware.HomeAssistant.NetDaemon.Common")));
            code = code.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("JoySoftware.HomeAssistant.NetDaemon.Common.Reactive")));

            // Add namespace
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nameSpace)).NormalizeWhitespace();

            // Add support for extensions for entities
            var extensionClass = SyntaxFactory.ClassDeclaration("GeneratedAppBase");

            extensionClass = extensionClass.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            extensionClass = extensionClass.AddBaseListTypes(
                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("NetDaemonRxApp")));

            // Get all available domains, this is used to create the extensionmethods
            var domains = GetDomainsFromEntities(entities);

            foreach (var domain in domains)
            {
                var camelCaseDomain = domain.ToCamelCase();
                var property = $@"public {camelCaseDomain}Entities {camelCaseDomain} => new {camelCaseDomain}Entities(this);";
                var propertyDeclaration = CSharpSyntaxTree.ParseText(property).GetRoot().ChildNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                extensionClass = extensionClass.AddMembers(propertyDeclaration);
        }
            namespaceDeclaration = namespaceDeclaration.AddMembers(extensionClass);

            foreach (var domain in GetDomainsFromEntities(entities))
            {
                var classDeclaration = $@"public partial class {domain.ToCamelCase()}Entity : RxEntity
    {{
        public string EntityId => EntityIds.First();

        public string? Area => DaemonRxApp.State(EntityId)?.Area;

        public dynamic? Attribute => DaemonRxApp.State(EntityId)?.Attribute;
        
        public DateTime LastChanged => DaemonRxApp.State(EntityId).LastChanged;

        public DateTime LastUpdated => DaemonRxApp.State(EntityId).LastUpdated;

        public dynamic? State => DaemonRxApp.State(EntityId)?.State;

        public {domain.ToCamelCase()}Entity(INetDaemonReactive daemon, IEnumerable<string> entityIds) : base(daemon, entityIds)
        {{          
        }}
    }}";
                var entityClass = CSharpSyntaxTree.ParseText(classDeclaration).GetRoot().ChildNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();

                // var entityIdProperty = $@"public string EntityId => EntityIds.First();";
                // var entityIdPropertyDeclaration = CSharpSyntaxTree.ParseText(entityIdProperty).GetRoot().ChildNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                // entityClass = entityClass.AddMembers(entityIdPropertyDeclaration);

                // var stateProperty = $@"public EntityState? State => DaemonRxApp.State(EntityId)?.State;";
                // var statePropertyDeclaration = CSharpSyntaxTree.ParseText(stateProperty).GetRoot().ChildNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                // entityClass = entityClass.AddMembers(statePropertyDeclaration);

                // They allready have default implementation
                var skipServices = new string[] {"turn_on", "turn_off", "toggle"};

                foreach (var s in services.Where(n => n.Domain == domain).SelectMany(n => n.Services))
                {
                    if (s.Service is null)
                        continue;

                    var name = s.Service[(s.Service.IndexOf(".") + 1)..];

                    if (Array.IndexOf(skipServices, name) >=0)
                        continue;

                    // Quick check to make sure the name is a valid C# identifier. Should really check to make
                    // sure it doesn't collide with a reserved keyword as well.
                    if (!char.IsLetter(name[0]) && (name[0] != '_'))
                    {
                        name = "s_" + name;
                    }
                    var hasEntityId = s.Fields.Count(c => c.Field == "entity_id") > 0? true : false;
                    var entityAssignmentStatement = hasEntityId? @"serviceData[""entity_id""] = EntityId;" : "";
                    var methodCode = $@"public void {name.ToCamelCase()}(dynamic? data=null)
                    {{
                        var serviceData = new FluentExpandoObject();

                        if (data is ExpandoObject)
                        {{
                            serviceData.CopyFrom(data);
                        }} 
                        else if (data is object)
                        {{
                            var expObject = ((object)data).ToExpandoObject();
                            serviceData.CopyFrom(expObject);
                        }}
                        {entityAssignmentStatement}
                        DaemonRxApp.CallService(""{domain}"", ""{s.Service}"", serviceData);
                    }}
                    ";
                    var methodDeclaration = CSharpSyntaxTree.ParseText(methodCode).GetRoot().ChildNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    entityClass = entityClass.AddMembers(methodDeclaration);
                }
                namespaceDeclaration = namespaceDeclaration.AddMembers(entityClass);

            }

            // Add the classes implementing the specific entities
            foreach (var domain in GetDomainsFromEntities(entities))
            {
    
                var classDeclaration = $@"public partial class {domain.ToCamelCase()}Entities
    {{
        private readonly NetDaemonRxApp _app;

        public {domain.ToCamelCase()}Entities(NetDaemonRxApp app)
        {{
            _app = app;
        }}
    }}";
                var entityClass = CSharpSyntaxTree.ParseText(classDeclaration).GetRoot().ChildNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                foreach (var entity in entities.Where(n => n.StartsWith(domain)))
                {
                    
                    var name = entity[(entity.IndexOf(".") + 1)..];
                    // Quick check to make sure the name is a valid C# identifier. Should really check to make
                    // sure it doesn't collide with a reserved keyword as well.
                    if (!char.IsLetter(name[0]) && (name[0] != '_'))
                    {
                        name = "e_" + name;
                    }

                    var propertyCode = $@"public {domain.ToCamelCase()}Entity {name.ToCamelCase()} => new {domain.ToCamelCase()}Entity(_app, new string[] {{""{entity}""}});";
                    var propDeclaration = CSharpSyntaxTree.ParseText(propertyCode).GetRoot().ChildNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
                    entityClass = entityClass.AddMembers(propDeclaration);
                }
                namespaceDeclaration = namespaceDeclaration.AddMembers(entityClass);
            }

            code = code.AddMembers(namespaceDeclaration);

            return code.NormalizeWhitespace(indentation: "    ", eol: "\n").ToFullString();
        }

        /// <summary>
        ///     Returns a list of domains from all entities
        /// </summary>
        /// <param name="entities">A list of entities</param>
        internal static IEnumerable<string> GetDomainsFromEntities(IEnumerable<string> entities) =>
            entities.Select(n => n[0..n.IndexOf(".")]).Distinct();
    }
}