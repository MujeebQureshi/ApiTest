using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web.Routing;
using TestAPI.Models.Utility;

namespace TestAPI.Models
{
    public class UserEP
    {
        public static object ValidateUser(string username, string password, string type)
        {
            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);

            try
            {
                password = DBManagerUtility._encodeJWT(new Dictionary<string, string>() { { "Password", password } }, AppConstants.AppSecretKeyPassword);

                // You can register the QueryFactory in the IoC container
                if (type == "USER")
                {
                    object response = db.Query("User").Where(q => q.Where("Email", username).OrWhere("Username", username))
                        .Where("Password", password)
                        .Where("RegistrationConfirmation", "Y").First();

                    var strResponse = response.ToString().Replace("DapperRow,", "").Replace("=", ":");
                    Dictionary<string, string> temp = JsonConvert.DeserializeObject<Dictionary<string, string>>(strResponse);
                    return temp;
                }
                else if (type == "ADMIN")
                {
                    object response = db.Query("Admin").Where("AdmUserId", username).Where("Password", password).First();
                    var strResponse = response.ToString().Replace("DapperRow,", "").Replace("=", ":");
                    Dictionary<string, string> temp = JsonConvert.DeserializeObject<Dictionary<string, string>>(strResponse);
                    return temp;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                //Logger.WriteErrorLog(ex);
                //return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                return null;
            }

        }

        public static object AddNewRegUser(RequestModel request)
        {
            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();

            db.Connection.Open();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));

                    object User;
                    test.TryGetValue("User", out User);
                    test.Remove("User");

                    object Document_SOE;
                    test.TryGetValue("Document_SOE", out Document_SOE);
                    List<string> _Document_SOE = Document_SOE as List<string>;
                    test.Remove("Document_SOE");
                    if (_Document_SOE != null && _Document_SOE.Count > 0)
                    {
                        //convert and add key    
                    }

                    if (User != null)
                    {


                        Dictionary<string, object> _User = JsonConvert.DeserializeObject<Dictionary<string, object>>(User.ToString());

                        //check if email exists
                        object Email;
                        _User.TryGetValue("Email", out Email);
                        string _Email = Email.ToString();

                        var response = db.Query("User").Where("Email", _Email).Get();
                        if (response != null && response.Count() > 0)
                        {
                            //return error
                            return new SuccessResponse(null, HttpStatusCode.Conflict, "Email already exists");
                        }

                        object Password;
                        _User.TryGetValue("Password", out Password);
                        string _Password = Password.ToString();
                        string _PasswordUnhashed = Password.ToString();

                        _User.Remove("Password");
                        if (!string.IsNullOrEmpty(_Password))
                        {
                            //convert and add key    
                            _Password = DBManagerUtility._encodeJWT(new Dictionary<string, string>() { { "Password", _Password } }, AppConstants.AppSecretKeyPassword);
                            _User.Add("Password", _Password);
                        }

                        _User.Add("RegistrationConfirmation", "Y");
                        _User.Add("isVerified", "N");

                        var query = db.Query("User").AsInsert(_User);

                        SqlKata.SqlResult compiledQuery = compiler.Compile(query);

                        //Inject the Identity in the Compiled Query SQL object
                        var sql = compiledQuery.Sql + "; SELECT @@IDENTITY as ID;";

                        //Name Binding house the values that the insert query needs 
                        var IdentityKey = db.Select<string>(sql, compiledQuery.NamedBindings).FirstOrDefault();

                        test.Add("UserId", IdentityKey);
                        var resRegUser = db.Query("RegisteredUser").Insert(test);

                        scope.Commit();

                        //testing
                        Dictionary<string, string> responseData = new Dictionary<string, string>();

                        #region issue AuthToken 
                        var pairs = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>( "grant_type", "password" ),
                            new KeyValuePair<string, string>( "username", _Email ),
                            new KeyValuePair<string, string> ( "Password", _PasswordUnhashed ),
                            new KeyValuePair<string, string> ( "scope", "USER" )
                        };

                        var content = new FormUrlEncodedContent(pairs);

                        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                        using (var client = new HttpClient())
                        {

                            var responseToken = client.PostAsync(Constants.BaseUrl + "token", content).Result;
                            var responseContent = responseToken.Content.ReadAsStringAsync().Result;
                            responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                            //return response.Content.ReadAsStringAsync().Result;
                        }
                        #endregion

                        bool hasData = true;//(response != null) ? true : false;
                        successResponseModel = new SuccessResponse(responseData, hasData);
                    }
                    else
                    {
                        scope.Rollback();
                    }
                }
                catch (Exception ex)
                {
                    //Logger.WriteErrorLog(ex);
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }
            }
            return successResponseModel;
        }

        public static object AddNewAdmin(RequestModel request)
        {
            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();

            db.Connection.Open();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
                    //check if email exists
                    object Email;
                    test.TryGetValue("AdmUserId", out Email);
                    string _Email = Email.ToString();

                    var response = db.Query("Admin").Where("AdmUserId", _Email).Get();
                    if (response != null && response.Count() > 0)
                    {
                        //return error
                        return new SuccessResponse(null, HttpStatusCode.Conflict, "Admin already exists");
                    }
                    object Password;
                    test.TryGetValue("Password", out Password);
                    string _Password = Password.ToString();
                    test.Remove("Password");
                    if (!string.IsNullOrEmpty(_Password))
                    {
                        //convert and add key    
                        _Password = DBManagerUtility._encodeJWT(new Dictionary<string, string>() { { "Password", _Password } }, AppConstants.AppSecretKeyPassword);
                        test.Add("Password", _Password);
                    }

                    var resRegUser = db.Query("Admin").Insert(test);

                    scope.Commit();
                    bool hasData = true;//(response != null) ? true : false;
                    successResponseModel = new SuccessResponse("", hasData);

                }
                catch (Exception ex)
                {
                    //Logger.WriteErrorLog(ex);
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }
            }
            return successResponseModel;
        }

        public static object AddNewUser(RequestModel request)
        {
            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();

            db.Connection.Open();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));

                    object User;
                    test.TryGetValue("User", out User);
                    test.Remove("User");

                    if (User != null)
                    {


                        Dictionary<string, object> _User = JsonConvert.DeserializeObject<Dictionary<string, object>>(User.ToString());

                        //check if email exists
                        object Email;
                        _User.TryGetValue("Email", out Email);
                        string _Email = Email.ToString();

                        var response = db.Query("User").Where("Email", _Email).Get();
                        if (response != null && response.Count() > 0)
                        {
                            //return error
                            return new SuccessResponse(null, HttpStatusCode.Conflict, "Email already exists");
                        }

                        object Password;
                        _User.TryGetValue("Password", out Password);
                        string _Password = Password.ToString();
                        string _PasswordUnhashed = _Password;
                        _User.Remove("Password");
                        if (!string.IsNullOrEmpty(_Password))
                        {
                            //convert and add key    
                            _Password = DBManagerUtility._encodeJWT(new Dictionary<string, string>() { { "Password", _Password } }, AppConstants.AppSecretKeyPassword);
                            _User.Add("Password", _Password);
                        }

                        _User.Add("RegistrationConfirmation", "N");
                        _User.Add("isVerified", "N");

                        var query = db.Query("User").AsInsert(_User);

                        SqlKata.SqlResult compiledQuery = compiler.Compile(query);

                        //Inject the Identity in the Compiled Query SQL object
                        var sql = compiledQuery.Sql + "; SELECT @@IDENTITY as ID;";

                        //Name Binding house the values that the insert query needs 
                        var IdentityKey = db.Select<string>(sql, compiledQuery.NamedBindings).FirstOrDefault();

                        //test.Add("UserId", IdentityKey);
                        //var resRegUser = db.Query("RegisteredUser").Insert(test);

                        scope.Commit();
                        Dictionary<string, string> responseData = new Dictionary<string, string>();

                        #region issue AuthToken 
                        var pairs = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>( "grant_type", "password" ),
                            new KeyValuePair<string, string>( "username", _Email ),
                            new KeyValuePair<string, string> ( "Password", _PasswordUnhashed ),
                            new KeyValuePair<string, string> ( "scope", "USER" )
                        };

                        var content = new FormUrlEncodedContent(pairs);

                        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                        using (var client = new HttpClient())
                        {

                            var responseToken = client.PostAsync(Constants.BaseUrl + "token", content).Result;
                            var responseContent = responseToken.Content.ReadAsStringAsync().Result;
                            responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);
                            //return response.Content.ReadAsStringAsync().Result;
                        }
                        #endregion

                        bool hasData = true;//(response != null) ? true : false;
                        successResponseModel = new SuccessResponse(responseData, hasData);
                    }
                    else
                    {
                        scope.Rollback();
                    }
                    //var response = db.Query("User").Insert(test);

                }
                catch (Exception ex)
                {
                    //Logger.WriteErrorLog(ex);
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }
            }
            return successResponseModel;
        }

        public static object AddUserRegDetails(RequestModel request)
        {
            var claims = ((ClaimsIdentity)Thread.CurrentPrincipal.Identity);
            var claim = claims.Claims.Where(x => x.Type == "UserId").FirstOrDefault();

            // Setup the connection and compiler
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();

            db.Connection.Open();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    if (claim != null && !string.IsNullOrEmpty(claim.Value))
                    {
                        var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));

                        test.Add("UserId", claim);
                        var resRegUser = db.Query("RegisteredUser").Insert(test);

                        var resUser = db.Query("User").Where("UserId", claim).Update(new
                        {
                            RegistrationConfirmation = "Y"
                        });

                        scope.Commit();
                        bool hasData = true;//(response != null) ? true : false;
                        successResponseModel = new SuccessResponse("", hasData);

                    }
                    else
                    {
                        return new SuccessResponse(null, HttpStatusCode.Unauthorized, "Please login to your account");
                    }
                }
                catch (Exception ex)
                {
                    //Logger.WriteErrorLog(ex);
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }
            }
            return successResponseModel;
        }

        public static object SendVerificationEmail(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));

            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                if (test != null)
                {
                    object Url;
                    test.TryGetValue("Url", out Url);
                    string _Url = Url.ToString();

                    object Email;
                    test.TryGetValue("Email", out Email);
                    string _Email = Email.ToString();

                    string encodedEmail = DBManagerUtility._encodeJWT(new Dictionary<string, string>() { { "Email", _Email } }, AppConstants.AppSecretLinkObject);

                    string link = Url + "?q=" + encodedEmail;

                    successResponseModel = new SuccessResponse(link, HttpStatusCode.OK, "Email sent successfully");
                }
                else
                {
                    successResponseModel = new SuccessResponse(null, HttpStatusCode.BadRequest, "Object is null");
                }

            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;

        }

        public static object Verify(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, object>>(Convert.ToString(request.RequestData));
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();

            try
            {
                if (test != null)
                {
                    object Token;
                    test.TryGetValue("Token", out Token);
                    string _Token = Token.ToString();
                    var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(DBManagerUtility._decodeJWT(_Token, AppConstants.AppSecretLinkObject) as string);

                    object Type;
                    test.TryGetValue("Type", out Type);
                    string _Type = Type.ToString();

                    object Email;
                    obj.TryGetValue("Email", out Email);
                    string _Email = Email.ToString();

                    var response = db.Query("User").Where("Email", _Email).First();
                    if (response != null)
                    {
                        if (_Type == "FPASS")
                        {
                            object newPassword;
                            test.TryGetValue("Password", out newPassword);
                            string _newPassword = newPassword.ToString();

                            _newPassword = DBManagerUtility._encodeJWT(new Dictionary<string, string>() { { "Password", _newPassword } }, AppConstants.AppSecretKeyPassword);

                            var resUser = db.Query("User").Where("Email", _Email).Update(new
                            {
                                Password = _newPassword
                            });

                            successResponseModel = new SuccessResponse(null, HttpStatusCode.OK, "Password Updated");
                        }
                        else if (_Type == "VERIFY")
                        {
                            var resUser = db.Query("User").Where("Email", _Email).Update(new
                            {
                                isVerified = "Y"
                            });

                            successResponseModel = new SuccessResponse(null, HttpStatusCode.OK, "User Verified");
                        }
                        else
                        {
                            successResponseModel = new SuccessResponse(null, HttpStatusCode.BadRequest, "Invalid Type");
                        }
                    }
                    else
                    {
                        successResponseModel = new SuccessResponse(null, HttpStatusCode.BadRequest, "Invalid Email");
                    }
                }
                else
                {
                    successResponseModel = new SuccessResponse(null, HttpStatusCode.BadRequest, "Object is null");
                }

            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;

        }
        //forgot password
        //change password

        public static object BuyShare(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object _PropertyID;
            test.TryGetValue("PropertyID", out _PropertyID);
            object TotalShareQuantity;
            test.TryGetValue("TotalShareQuantity",out TotalShareQuantity);
            int _totalShareQuantity = Convert.ToInt32(TotalShareQuantity);
            object ShareQty;
            test.TryGetValue("ShareQty", out ShareQty);
            int _ShareQty = Convert.ToInt32(ShareQty);
            object ShareMarketValue;
            test.TryGetValue("ShareBuyingValue", out ShareMarketValue);
            object TotalAmount;
            test.TryGetValue("ShareAmount", out TotalAmount);
            object RegUserID;
            test.TryGetValue("RegUserID", out RegUserID);

            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            db.Connection.Open();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    //object response;
                    var response = db.Query("UserShare")
                               .SelectRaw("SUM(`ShareQty`) as SumShares")
                               .Where("PropertyID", _PropertyID)
                               .Get()
                               .Cast<IDictionary<string, object>>();
                    //total share -usershare-sharehold
                    int sumShare = 0;
                    if (response != null)
                        sumShare = response.ElementAt(0)["SumShares"] == null ? 0 : Convert.ToInt32(response.ElementAt(0)["SumShares"]);
                    response = db.Query("PropertyShareHold")
                               .Select("PropertyID", "ShareQty")
                               .SelectRaw("MAX(`URN`) as MaxURN")
                               .Where("PropertyID", _PropertyID)
                               .Get()
                               .Cast<IDictionary<string, object>>();
                    int holdShare = 0;
                    if (response != null)
                    {
                        holdShare = response.ElementAt(0)["ShareQty"] == null ? 0 : Convert.ToInt32(response.ElementAt(0)["ShareQty"]);

                    }
                    int remainingShares = _totalShareQuantity - holdShare - sumShare;
                    if (remainingShares > _ShareQty)
                    {
                        Dictionary<string, object> UserShareRecord = new Dictionary<string, object>() { { "PropertyID", _PropertyID },
                                                                                                    {"RegUserID", RegUserID},
                                                                                                    {"DateofInvestment", DateTime.Now.Date },
                                                                                                    {"ShareMarketValue",ShareMarketValue},
                                                                                                    {"ShareStatus","H" },
                                                                                                    {"TotalAmount",TotalAmount},
                                                                                                    {"ShareQty",ShareQty}
                                                                                                 };
                        remainingShares = remainingShares - _ShareQty;
                        var res1= db.Query("PropertyShare").Where("PropertyID", _PropertyID).Update( new { AvailableShares = remainingShares }) ;
                        var res = db.Query("UserShare").Insert(UserShareRecord);
                        bool hasData = true;
                        scope.Commit();
                        successResponseModel = new SuccessResponse(res, hasData);

                    }
                    else
                    {
                        //handle when remaining share is less

                    }


                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }
                return successResponseModel;
            }
        }
        
        public static object GetUserShares (RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object RegUserID;
            test.TryGetValue("RegUserID", out RegUserID);
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            try
            {
                var response = db.Query("UserShare as U")
                    .Select("PropertyDetail.PropertyName as PropertyName", "PropertyDetail.PropertyType as PropertyType","U.ShareQty as ShareQty",
                    "U.DateofInvestment as DateofInvestment", "U.ShareMarketValue as ShareMarketValue", "U.TotalAmount as TotalAmount"  )
                    .Join("PropertyDetail", "PropertyDetail.PropertyID", "U.PropertyID")
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
        public static object GetUserSharesOnHold(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object RegUserID;
            test.TryGetValue("RegUserID", out RegUserID);
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            try
            {
                var response = db.Query("UserShare as S")
                    .Select("U.FirstName as FirstName","U.LastName as LastName","PropertyDetail.PropertyName as PropertyName", "PropertyDetail.PropertyType as PropertyType",
                    "S.UserShareID as UserShareID", "S.ShareQty as ShareQty",
                    "S.DateofInvestment as DateofInvestment", "S.ShareMarketValue as ShareMarketValue", "S.TotalAmount as TotalAmount")
                    .Join("PropertyDetail", "PropertyDetail.PropertyID", "S.PropertyID")
                    .Join("RegisteredUser as R", "R.RegUserID", "S.RegUserID")
                    .Join("User as U" ,"R.UserID","U.UserID")
                    .Where("S.ShareStatus","H")
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
        public static object EditShareStatus(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object _UserShareID;
            test.TryGetValue("UserShareID", out _UserShareID);
            object _ShareStatus;
            test.TryGetValue("ShareStatus", out _ShareStatus);
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            db.Connection.Open();
            SuccessResponse successResponseModel = new SuccessResponse();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    //to update available shares
                    if (_ShareStatus.ToString() == "A")
                    { var res1 = db.Query("UserShare").Select("ShareQty","PropertyID").Where("UserShareID", _UserShareID).Get().Cast<IDictionary<string, object>>();
                        int ShareQty = Convert.ToInt32(res1.ElementAt(0)["ShareQty"]);
                        int PropertyID = Convert.ToInt32(res1.ElementAt(0)["PropertyID"]);
                        var res2 = db.Query("PropertyShare").Select("AvailableShares").Where("PropertyID", PropertyID).Get().Cast<IDictionary<string, object>>();
                        int availableShares= Convert.ToInt32(res2.ElementAt(0)["AvailableShares"]);
                        availableShares = availableShares - ShareQty;
                        var res3 = db.Query("PropertyShare").Where("PropertyID", PropertyID).Update(new { availableShares = availableShares });
                    }
                    var response = db.Query("UserShare").Where("UserShareID", _UserShareID).Update(test);

                    bool hasData = true;
                    scope.Commit();
                    successResponseModel = new SuccessResponse(response, hasData);
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }

                return successResponseModel;
            }
        }

        public static object AddUserInterested (RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            db.Connection.Open();
            using (var scope = db.Connection.BeginTransaction())
            {
                try
                {
                    var response = db.Query("User_Interested").Insert(test);

                    bool hasData = true;
                    scope.Commit();
                    successResponseModel = new SuccessResponse(response, hasData);
                }
                catch (Exception ex)
                {
                    scope.Rollback();
                    return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
                }

                return successResponseModel;
            }
        }
        public static object GetUserInterested(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object _UserID;
            test.TryGetValue("UserID", out _UserID);
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            try
            {
                  var response = db.Query("User_Interested as I")
                       .Select("PropertyDetail.PropertyName as PropertyName", "Propertydetail.PropertyType as PropertyType", "I.LastfiledDateTime","I.ValueOnCurrentDate")
                       .Join("Propertydetail", "Propertydetail.PropertyID", "I.PropertyID")
                       .Where("I.UserID", _UserID)
                       .Get(); 
              

                bool hasData = true;
                successResponseModel = new SuccessResponse(response, hasData);
            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;

        }

//get all users investments for property        
        public static object GetAllUsersforProperty (RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object PropertyID;
            test.TryGetValue("PropertyID", out PropertyID);
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            try
            {
                var response = db.Query("UserShare as S")
                   .Select("U.UserName as UserName", "S.DateofInvestment as DateInvested",
                   "S.ShareQty as ShareQty", "S.ShareMarketValue as ShareMarketValue", "S.TotalAmount as TotalAmount")
                   .Join("RegisteredUser as R", "R.RegUserID", "S.RegUserID")
                   .Join("User as U", "R.UserID", "U.UserID")
                   .Where("S.ShareStatus", "<>", "H")
                   .Where("S.PropertyID", PropertyID)
                   .OrderBy("S.RegUserID")
                   .OrderBy("S.PropertyID")
                   .OrderByDesc("S.DateofInvestment")
                   .Get();
                bool hasData = true;
                successResponseModel = new SuccessResponse(response, hasData);

            }
            catch (Exception ex)
            {
                return new ErrorResponse(ex.Message, HttpStatusCode.BadRequest);
            }

            return successResponseModel;

        }
        public static object GetAllPropertyforUsers(RequestModel request)
        {
            var test = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Convert.ToString(request.RequestData));
            object RegUserID;
            test.TryGetValue("RegUserID", out RegUserID);
            var connection = new MySqlConnection(ConfigurationManager.AppSettings["MySqlDBConn"].ToString());
            var compiler = new MySqlCompiler();
            var db = new QueryFactory(connection, compiler);
            SuccessResponse successResponseModel = new SuccessResponse();
            try
            {
                var response = db.Query("UserShare as S")
                   .Select("P.PropertyName as PropertyName", "P.PropertyType","S.DateofInvestment as DateInvested",
                   "S.ShareQty as ShareQty", "S.ShareMarketValue as ShareMarketValue", "S.TotalAmount as TotalAmount")
                   .Join("PropertyDetail as P", "P.PropertyID", "S.PropertyID")
                   .Where("S.ShareStatus", "<>", "H")
                   .Where("S.RegUserID", RegUserID)
                   .OrderBy("S.PropertyID")
                   .OrderByDesc("S.DateofInvestment")
                   .Get();
                bool hasData = true;
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
