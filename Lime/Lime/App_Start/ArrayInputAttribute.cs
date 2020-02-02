using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Lime.App_Start
{
    public class ArrayInputAttribute : ActionFilterAttribute
    {
        private readonly string[] _parameternames;

        public string Separator { get; set; }


        public ArrayInputAttribute(params string[] parameternames)
        {
            _parameternames = parameternames;
            Separator = "-";
        }



        public void ProcessArrayInput(HttpActionContext actionContext, string parametername)
        {
            if (actionContext.ActionArguments.ContainsKey(parametername))
            {
                var parameterdescriptor = actionContext.ActionDescriptor.GetParameters().FirstOrDefault(p => p.ParameterName == parametername);

                if (parameterdescriptor != null && parameterdescriptor.ParameterType.IsArray)
                {
                    var type = parameterdescriptor.ParameterType.GetElementType();
                    var parameters = String.Empty;
                    if (actionContext.ControllerContext.RouteData.Values.ContainsKey(parametername))
                    {
                        parameters = (string)actionContext.ControllerContext.RouteData.Values[parametername];
                    }
                    else
                    {
                        var queryString = actionContext.ControllerContext.Request.RequestUri.ParseQueryString();
                        if (queryString[parametername] != null)
                        {
                            parameters = queryString[parametername];
                        }
                    }

                    var values = parameters.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(TypeDescriptor.GetConverter(type).ConvertFromString).ToArray();
                    var typedValues = Array.CreateInstance(type, values.Length);
                    values.CopyTo(typedValues, 0);
                    actionContext.ActionArguments[parametername] = typedValues;
                }
            }
        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {

            foreach (var parameterName in _parameternames)
            {
                ProcessArrayInput(actionContext, parameterName);
            }
        }
    }
}