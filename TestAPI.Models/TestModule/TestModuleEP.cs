using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using TestAPI.Models.Utility;
using System.Transactions;

namespace TestAPI.Models
{
    public class TestModuleEP
    {
        public static object GetDemoList(RequestModel request)
        {
            //var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));

            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler(); 
            var db = new QueryFactory(connection, compiler);

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                // You can register the QueryFactory in the IoC container
                var response = db.Query("User").Get();
                //db.Query("jpexperience").Where("ExpId", 6).Where("ProfileId", 4).First();
                bool hasData = (response != null) ? true : false;
                successResponseModel = new SuccessResponse(response, hasData);
            }
            catch (Exception ex)
            {
                //Logger.WriteErrorLog(ex);
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;
            
        }

        public static object GetCustomList(RequestModel request)
        {
            //var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                // You can register the QueryFactory in the IoC container
                string strRawQuery = @"
                    (select * from jpopening where Department = 'Human Resource' limit 1)
                    union all
                    (select * from jpopening where Department = 'Marketing' limit 1)
                    union all
                    (select * from jpopening where Department = 'IT' limit 1)
                ";
                var response = db.Select(strRawQuery);
                
                bool hasData = (response != null) ? true : false;
                successResponseModel = new SuccessResponse(response, hasData);
            }
            catch (Exception ex)
            {
                //Logger.WriteErrorLog(ex);
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;
        }
        
        public static object SaveDemoRecord(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));

            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                //You can register the QueryFactory in the IoC container
                var response = db.Query("propertydetail").Insert(test);
                bool hasData = true;
                successResponseModel = new SuccessResponse(response, hasData);
            }
            catch (Exception ex)
            {
                //Logger.WriteErrorLog(ex);
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;
        }
    }
}
