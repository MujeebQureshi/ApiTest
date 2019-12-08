using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using TestAPI.Models.Utility;

namespace TestAPI.Models
{
    public class DevelopmentalEP
    {        
        public static object GetDevPropList(RequestModel request)
        {
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                var response = db.Query("developmental").Select("propertydetail.propertyname","developmental.startdate")
                    .Join("propertydetail", "propertydetail.propertyid","developmental.propertyid")
                    .Get(); 
                bool hasData = (response != null) ? true : false;
                successResponseModel = new SuccessResponse(response, hasData);
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;

        }
    }
}
