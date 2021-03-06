//********************************************************************************************
//Author: Sergey Stoyan
//        sergey.stoyan@gmail.com
//        http://www.cliversoft.com
//********************************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Script.v1;
using Google.Apis.Script.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.Requests;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace Cliver
{
    public partial class GoogleScript : GoogleService<ScriptService>
    {
        public GoogleScript(string applicationName, IEnumerable<string> scopes, string scriptId, IDataStore dataStore, string clientSecretFile = null)
        {
            Credential = GoogleRoutines.GetCredential(applicationName, scopes, dataStore, clientSecretFile);
            service = new ScriptService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential,
                ApplicationName = applicationName,
            });
            ScriptId = scriptId;
        }

        public GoogleScript(string applicationName, IEnumerable<string> scopes, string scriptId, string credentialDir = null, string clientSecretFile = null)
        {
            if (credentialDir == null)
                credentialDir = Log.AppCompanyUserDataDir + "\\googleScriptCredential";
            Credential = GoogleRoutines.GetCredential(applicationName, scopes, credentialDir, clientSecretFile);
            service = new ScriptService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential,
                ApplicationName = applicationName,
            });
            ScriptId = scriptId;
        }

        public readonly string ScriptId;

        public int RetryMaxCount = 3;
        public int RetryDelayMss = 10000;

        public object Run(string function, params object[] parameters)
        {
            ExecutionRequest request = new ExecutionRequest
            {
                Function = function,
                Parameters = parameters,
#if DEBUG
                DevMode = true,
#else
                DevMode = false,
#endif
            };
            ScriptsResource.RunRequest runRequest = service.Scripts.Run(request, ScriptId);
            Operation operation = null;
            for (int i = 0; ; i++)
                try
                {
                    operation = runRequest.Execute();
                    break;
                }
                catch (Google.GoogleApiException e)
                {
                    if (i >= RetryMaxCount)
                        throw;
                    if (e.Error?.Code != 500)
                        throw;
                    Log.Warning2("Retrying...", e);
                    System.Threading.Thread.Sleep(RetryDelayMss);
                }
            if (operation.Error != null)
            {
                string message = "Server error: " + operation.Error.ToStringByJson();
                throw new Exception(message);
            }
            return operation.Response["result"];
        }

        public class Exception : System.Exception
        {
            public Exception(string message) : base(message) { }
        }

        ///// <summary>
        ///// Requires the following OAuth scope: https://www.googleapis.com/auth/script.projects
        ///// </summary>
        ///// <param name="scriptTitle"></param>
        ///// <param name="scriptFiles"></param>
        ///// <param name="parentId">The Drive ID of a parent file that the created script project is bound to. 
        ///// This is usually the ID of a Google Doc, Google Sheet, Google Form, or Google Slides file. 
        ///// If not set, a standalone script project is created.</param>
        ///// <returns></returns>
        //public object Deploy(string scriptTitle, List<string> scriptFiles, string parentId = null)
        //{
        //    CreateProjectRequest request = new CreateProjectRequest
        //    {
        //        Title = scriptTitle,
        //         ParentId=parentId
        //    };
        //  ProjectsResource.CreateRequest createRequest = scriptService.Projects.Create(request);
        //    Project project = createRequest.Execute();
        //    //if (project== null)
        //    //{
        //    //    string message = "Server error: " + operation.Error.ToStringByJson();
        //    //    throw new Exception2(message);
        //    //}
        //    Content content = new Content { Files = scriptFiles, ScriptId = project.ScriptId };
        //    ProjectsResource.UpdateContentRequest updateContentRequest = scriptService.Projects.UpdateContent((updateContentRequest)
        //    project.u
        //}
    }
}