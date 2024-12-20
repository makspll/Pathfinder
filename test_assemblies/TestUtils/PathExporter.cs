


using Newtonsoft.Json;

#if NETCOREAPP || NET8_0_OR_GREATER
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

#elif NET472
using ActionNameAttribute = System.Web.Mvc.ActionNameAttribute;
using System.Collections.Specialized;
using System.Web;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;
using System.Web.Routing;
using System.Web.Http;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.WebHost.Routing;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using System.Web.Http.WebHost;
using System;
using System.Web.Mvc;
using System.Web.Mvc.Async;
#endif


namespace TestUtils
{


    public class RouteInfo
    {
        public RouteInfo()
        {
            HttpMethods = new List<string>();
            Routes = new List<string>();
        }

        public RouteInfo(IEnumerable<string> httpMethods, IEnumerable<string> routes)
        {
            HttpMethods = httpMethods;
            Routes = routes;
        }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("httpMethods")]
#endif
        [JsonProperty("httpMethods")]
        public IEnumerable<string> HttpMethods { get; set; }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("routes")]
#endif
        [JsonProperty("routes")]
        public IEnumerable<string> Routes { get; set; }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("action")]
#endif
        [JsonProperty("action")]
        public string? Action { get; set; }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("actionMethodName")]
#endif
        [JsonProperty("actionMethodName")]
        public string? ActionMethodName { get; set; }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("controllerName")]
#endif
        [JsonProperty("controllerName")]
        public string? ControllerName { get; set; }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("controllerClassName")]
#endif
        [JsonProperty("controllerClassName")]
        public string? ControllerClassName { get; set; }

#if NETCOREAPP || NET8_0_OR_GREATER
        [JsonPropertyName("controllerNamespace")]
#endif
        [JsonProperty("controllerNamespace")]
        public string? ControllerNamespace { get; set; }

        public RouteInfo Merge(RouteInfo other)
        {
            if (ControllerName != other.ControllerName || Action != other.Action)
            {
                throw new Exception($"Cannot merge `{this}` and `{other}`. Since the controller and/or actions don't match");
            }

            HttpMethods = HttpMethods.Union(other.HttpMethods);
            Routes = Routes.Union(other.Routes);
            return this;
        }

        public override string ToString()
        {
            var actionString = Action != ActionMethodName ? $"{ActionMethodName}({Action})" : ActionMethodName;
            var controllerString = ControllerName != ControllerClassName ? $"{ControllerClassName}({ControllerName})" : ControllerClassName;
            var httpMethodsString = string.Join(", ", HttpMethods ?? new List<string>());
            var routes = string.Join(", ", Routes ?? new List<string>());
            return $"{ControllerNamespace}::{controllerString}::{actionString} - {httpMethodsString} - [{routes}]";
        }
    }

#if NETCOREAPP

    public static class PathsExporter
    {
        public static List<RouteInfo> ListAllRoutes(IEnumerable<ControllerActionDescriptor> _endpointSources, IEnumerable<EndpointDataSource> endpointSources, bool includeConventional = true, bool includeAttributeRoutes = true)
        {
            var output = _endpointSources.Select(
                controller =>
                {
                    var action = controller?.ActionName;

                    var controllerName = controller?.ControllerTypeInfo.Name;
                    var controllerClassName = controllerName;
                    if (controllerName?.EndsWith("Controller") ?? false)
                        controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);

                    var controllerNamespace = controller?.ControllerTypeInfo.Namespace;
                    var actionMethodName = controller?.MethodInfo.Name;


                    var route = controller?.AttributeRouteInfo?.Template;
                    string[] allHttpMethods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
                    var httpMethods = controller!.ActionConstraints?.OfType<HttpMethodActionConstraint>().SelectMany(r => r.HttpMethods) ?? Enumerable.Empty<string>();
                    if (httpMethods.Count() == 0)
                        httpMethods = allHttpMethods;


                    if (route == null && !includeConventional)
                        return null;
                    if (route != null && !includeAttributeRoutes)
                        return null;

                    if (route != null && !route.StartsWith("/"))
                        route = "/" + route;

                    List<string> all_routes = route == null ? new List<string>() { } : new List<string> { route };
                    
                    if (route == null)
                    {
                        // figure out which conventional routes route to this controller

                        var all_conventional_routes = endpointSources
                            .SelectMany(e => e.Endpoints)
                            .OfType<RouteEndpoint>()
                            .Where(e =>
                            {
                                var routeControllerName = e.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.Name;
                                var routeActionName = e.Metadata.GetMetadata<ControllerActionDescriptor>()?.ActionName;
                                if (e.RoutePattern.Defaults.Count == 0 && e.RoutePattern.Parameters.Count == 0)
                                    return false;

                                var hasControllerParameterKey = e.RoutePattern.Parameters.Any(p => p.Name == "controller");
                                var hasActionParameterKey = e.RoutePattern.Parameters.Any(p => p.Name == "action");

                                if (routeActionName == null)
                                {
                                    // try find parameters in the route pattern as well 
                                    routeActionName = (string)(e.RoutePattern.Parameters
                                        .Where(p => p.Name == "action")
                                        .Select(p => p.Default)
                                        .FirstOrDefault() ?? e.RoutePattern.Defaults.Where(p => p.Key == "action").Select(p => p.Value).FirstOrDefault() ?? "");
                                }
                                if (routeControllerName == null)
                                {
                                    // try find parameters in the route pattern as well 
                                    routeControllerName = (string)(e.RoutePattern.Parameters
                                        .Where(p => p.Name == "controller")
                                        .Select(p => p.Default)
                                        .FirstOrDefault() ?? e.RoutePattern.Defaults.Where(p => p.Key == "controller").Select(p => p.Value).FirstOrDefault() ?? "");
                                }


                                return controllerName == routeControllerName && routeActionName == action;

                            })
                            .Select(e => e.RoutePattern.RawText)
                            .ToList();
                        all_routes = all_conventional_routes!;
                    }

                    // for attribute routes with placeholders

                    all_routes = all_routes.Select(x => 
                        x.Replace("{{", "Ѻ").Replace("}}", "ѻ")
                            .Replace("{controller}", controllerName)
                            .Replace("{action}", action)
                            .Replace("Ѻ", "{").Replace("ѻ", "}")
                    ).ToList();

                    return new RouteInfo(httpMethods, all_routes)
                    {
                        Action = action,
                        ActionMethodName = actionMethodName,
                        ControllerName = controllerName,
                        ControllerNamespace = controllerNamespace,
                        ControllerClassName = controllerClassName
                    };
                }
            ).OfType<RouteInfo>().ToList();

            output.ForEach(x => {
                x.Routes = x.Routes.Select(r => {
                    if (!r.StartsWith("/")){
                        r = "/" + r;
                    }
                    if (r.EndsWith("/"))
                        r = r.Substring(0, r.Length - 1);
                    return r;
                });
            });

            return output.ToList();;
        }
    }
#elif NET472
    public static class PathExporter
    {
        private static List<string> _ALL_METHODS =
            new()
            {
                HttpMethod.Get.Method, HttpMethod.Put.Method, HttpMethod.Delete.Method, HttpMethod.Head.Method, HttpMethod.Options.Method,
                HttpMethod.Post.Method, HttpMethod.Trace.Method, "PATCH"
            };

        public static List<RouteInfo> ListAllRoutes(bool includeConventional = true, bool includeAttributeRoutes = true)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            var output = new List<RouteInfo>();

            var explorer = new ApiExplorer(GlobalConfiguration.Configuration);

            foreach (var routeMethod in explorer.ApiDescriptions)
            {
                var isAttributeRouted =
                    routeMethod.ActionDescriptor.Properties.Any(x =>
                        (string)x.Key == "MS_IsAttributeRouted" && (bool)x.Value) ||
                    routeMethod.ActionDescriptor.ControllerDescriptor.Properties.Any(x =>
                        (x.Key as string) == "MS_IsAttributeRouted" && (bool)x.Value);
                var isConventional = !isAttributeRouted;
                if (!includeAttributeRoutes && isAttributeRouted)
                    continue;
                if (!includeConventional && isConventional)
                    continue;

                var actionName = routeMethod.ActionDescriptor.ActionName;
                var actionMethodName = "unknown";
                if (routeMethod.ActionDescriptor is ReflectedHttpActionDescriptor d)
                {
                    actionMethodName = d.MethodInfo.Name;
                }

                var controllerName = routeMethod.ActionDescriptor.ControllerDescriptor.ControllerName;
                var controllerClassName = routeMethod.ActionDescriptor.ControllerDescriptor.ControllerType.Name;
                var controllerNamespace =
                    routeMethod.ActionDescriptor.ControllerDescriptor.ControllerType.Namespace;
                var routes = new List<string>() { routeMethod.RelativePath };
                var methods = new List<string>() { routeMethod.HttpMethod.ToString() };
                var routeInfo = new RouteInfo(methods, routes)
                {
                    Action = actionName,
                    ActionMethodName = actionMethodName,
                    ControllerName = controllerName,
                    ControllerClassName = controllerClassName,
                    ControllerNamespace = controllerNamespace,
                };
                output.Add(routeInfo);
            }

            var coalesced = output.GroupBy(x => (x.ControllerClassName, x.ActionMethodName)).Select(x =>
            {
                using var enumerator = x.GetEnumerator();
                enumerator.MoveNext();
                var mergeOutput = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    mergeOutput = mergeOutput!.Merge(enumerator.Current!);
                }

                return mergeOutput;
            }).ToList();


            // Process MVC routes (apparently a different routing system REEREE)
            var attributeMVCRoutes = RouteTable.Routes.SelectMany(x =>
            {
                if (x.GetType().Name == "RouteCollectionRoute")
                {
                    var subRoutes = x.GetType()
                        .GetField("_subRoutes", BindingFlags.NonPublic | BindingFlags.Instance);
                    IReadOnlyCollection<RouteBase> attrRoutes = (IReadOnlyCollection<RouteBase>)subRoutes.GetValue(x);
                    return attrRoutes.ToList();
                }
                else
                {
                    return new List<RouteBase>();
                }
            }).ToList();


            var attributeMvcRouteInfos = attributeMVCRoutes.OfType<Route>().Select(ParseAttributeRoute).ToList();
            var conventionalMVCRoutes = RouteTable.Routes
                    .OfType<Route>()
                    .Where(x => x.GetType() == typeof(Route))
                    .SelectMany(x => ParseConventionalRoute(x, callingAssembly))
                    .Where(x => !attributeMvcRouteInfos.Any(o => o.ControllerClassName == x.ControllerClassName && o.ActionMethodName == x.ActionMethodName))
                    .ToList();


            output = coalesced.ToList();
            if (includeAttributeRoutes)
                output = output.Concat(attributeMvcRouteInfos).ToList();
            if (includeConventional)
                output = output.Concat(conventionalMVCRoutes).ToList();

            // when checking for http methods we also iterate through all action selectors
            // this includes filters like NonAction
            // if no http methods are allowed, we have ignored the action via attribute
            output = output.Where(x => x.HttpMethods.Any()).ToList();

            output.ForEach(x =>
            {
                x.Routes = x.Routes.Select(r =>
                {
                    if (!r.StartsWith("/"))
                    {
                        r = "/" + r;
                    }
                    if (r.EndsWith("/"))
                        r = r.Substring(0, r.Length - 1);
                    return r;
                });
            });

            return output;
        }


        public static List<RouteInfo> ParseConventionalRoute(Route route, Assembly callingAssembly)
        {

            // Exactly how this is done in mvc
            // https://github.com/jbogard/aspnetwebstack/blob/730c683da2458430d36e3e360aba68932ba69fa4/src/System.Web.Mvc/ControllerTypeCache.cs#L86C1-L95C1
            var controllers = callingAssembly.GetTypes().Where(t =>
                    t.IsPublic &&
                    t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) &&
                    !t.IsAbstract &&
                    typeof(System.Web.Mvc.IController).IsAssignableFrom(t)
            ).ToList();

            // first if the route does not contain controller placeholder we only check one controller and that is the default one
            if (!route.Url.Contains("{controller}"))
            {
                var defaultController = controllers.FirstOrDefault(x => x.Name.Substring(0, x.Name.Length - "Controller".Length) == (string)route.Defaults["controller"]);
                if (defaultController == null)
                    return new List<RouteInfo>();
                controllers = new List<Type>() { defaultController };
            }

            route.Defaults.TryGetValue("action", out var defaultActionObject);
            var stringDefaultAction = (string?)defaultActionObject;
            if (!route.Url.Contains("{action}"))
            {
                // if the route does not constraint the action we need a default route or we return no routes
                if (stringDefaultAction == null)
                {
                    return new List<RouteInfo>();
                }
            }
            else
            {
                // override default if we don't care
                stringDefaultAction = null;
            }

            var routeInfos = new List<RouteInfo>();
            // now for each available action and controller pair, generate a route
            foreach (var controllerType in controllers)
            {
                var availableActions = new List<ReflectedActionDescriptor>() { };
                var controllerDescriptor = new ReflectedControllerDescriptor(controllerType);

                // get available actions
                var actions = controllerDescriptor.GetCanonicalActions().Select(x => (ReflectedActionDescriptor)x);

                if (stringDefaultAction == null)
                {
                    availableActions = actions.ToList();
                }
                else
                {
                    var defaultAction = actions.FirstOrDefault(x => x.ActionName == stringDefaultAction);
                    if (defaultAction == null)
                        return new List<RouteInfo>();
                    availableActions = new List<ReflectedActionDescriptor>() { defaultAction };
                }

                foreach (var action in availableActions)
                {
                    var actionName = GetConventionalMvcActionName(action);
                    var instantiatedRoute = route.Url
                        .Replace("{controller}", controllerDescriptor.ControllerName)
                        .Replace("{action}", actionName);

                    var routes = new List<string>() { instantiatedRoute };
                    var routeInfo = new RouteInfo(GetAllowedHttpMethods(action), routes)
                    {
                        Action = actionName,
                        ControllerName = controllerDescriptor.ControllerName,
                        ControllerClassName = controllerDescriptor.ControllerType.Name,
                        ControllerNamespace = controllerDescriptor.ControllerType.Namespace,
                        ActionMethodName = action.MethodInfo.Name
                    };
                    routeInfos.Add(routeInfo);
                }
            }

            return routeInfos.ToList();
        }

        public static RouteInfo ParseAttributeRoute(Route route)
        {
            var actionDescriptors = (ActionDescriptor[])route.DataTokens["MS_DirectRouteActions"];
            var actionDescriptor = actionDescriptors.SingleOrDefault();

            var controllerDescriptor = actionDescriptor!.ControllerDescriptor;
            var routes = new List<string>() { route.Url };
            var methods = new List<string>();
            var actionMethod = "unknown";
            var MethodInfo = actionDescriptor.GetType().GetRuntimeProperty("MethodInfo");
            if (MethodInfo != null)
            {
                actionMethod = ((MethodInfo)MethodInfo.GetValue(actionDescriptor)).Name;
            }

            var allowedMethods = GetAllowedHttpMethods(actionDescriptor);
            return new RouteInfo(allowedMethods, routes)
            {
                Action = actionDescriptor.ActionName,
                ActionMethodName = actionMethod,
                ControllerName = controllerDescriptor.ControllerName,
                ControllerClassName = controllerDescriptor.ControllerType.Name,
                ControllerNamespace = controllerDescriptor.ControllerType.Namespace,

            };
        }

        public static List<string> GetAllowedHttpMethods(ActionDescriptor descriptor)
        {
            var methods = new List<string>();

            if (descriptor.GetSelectors().Count == 0)
            {
                methods = _ALL_METHODS.Select(httpMethod => httpMethod.ToString()).ToList();
                return methods;
            }

            foreach (var method in _ALL_METHODS)
            {
                var httpRequest = new MockHttpRequest(method);
                var context = new MockHttpContext(httpRequest);
                var controllerContext = new MockControllerContext(context);
                if (descriptor.GetSelectors().All(x => x(controllerContext)))
                    methods.Add(method);
            }

            return methods;
        }

        public static string GetConventionalMvcActionName(ActionDescriptor descriptor)
        {
            var overrides = descriptor.GetCustomAttributes(true).OfType<ActionNameAttribute>().ToList();

            if (overrides.FirstOrDefault() is { } a)
                return a.Name;

            return descriptor.ActionName;
        }

        // public static string GetActionName(ActionDescriptor descriptor)
        // {
        //     var overrides = descriptor.GetCustomAttributes(true).OfType<ActionNameSelectorAttribute>();
        //
        //     if (overrides.Any())
        //     {
        //         overrides.First().IsValidName()
        //     }
        // }

    }


    public class MockHttpContext : HttpContextBase
    {
        private readonly HttpRequestBase _request;

        public MockHttpContext(MockHttpRequest request)
        {
            _request = request;
        }

        public override HttpRequestBase Request => _request;
    }

    public class MockHttpRequest : HttpRequestBase
    {
        private readonly string _httpMethod;
        private readonly NameValueCollection _headers;


        public MockHttpRequest(string httpMethod)
        {
            _httpMethod = httpMethod;
            _headers = new NameValueCollection();
            _headers.Add("X-HTTP-Method-Override", httpMethod);
        }

        public override string HttpMethod => _httpMethod;
        public override NameValueCollection Headers => _headers;
    }

    public class MockControllerContext : ControllerContext
    {
        private readonly MockHttpContext Context;

        public MockControllerContext(MockHttpContext context)
        {
            this.Context = context;
        }

        public override HttpContextBase HttpContext => Context;
    }


#endif

}