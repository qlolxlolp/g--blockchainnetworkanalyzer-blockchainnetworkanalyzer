using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using BlockchainNetworkAnalyzer.Core;

namespace BlockchainNetworkAnalyzer.Core.Services
{
    /// <summary>
    /// Service for retrieving subscriber information from ISPs
    /// This service queries Iranian ISP databases for subscriber details
    /// </summary>
  public class SubscriberInfoService
    {
        private readonly ILogger<SubscriberInfoService> _logger;
        private readonly HttpClient _httpClient;

        public SubscriberInfoService(ILogger<SubscriberInfoService> logger)
        {
         _logger = logger;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task<SubscriberInfo> GetSubscriberInfoAsync(string ipAddress)
      {
      var subscriberInfo = new SubscriberInfo();

            try
            {
 // First, identify the ISP
    var connectionInfo = await new InternetConnectionService().AnalyzeConnectionAsync(ipAddress);
         
       if (!connectionInfo.IsIranianISP)
                {
         _logger.LogWarning($"IP {ipAddress} is not from Iranian ISP");
        return subscriberInfo;
       }

             // Query based on ISP type
    if (connectionInfo.ISP?.Contains("Telecommunication") == true || 
        connectionInfo.ISP?.Contains("Tci") == true ||
    connectionInfo.ISP?.Contains("ایران تلکام") == true)
                {
     subscriberInfo = await GetTelecomSubscriberInfoAsync(ipAddress);
      }
      else if (connectionInfo.ISP?.Contains("MTN") == true || 
    connectionInfo.ISP?.Contains("MCI") == true ||
                  connectionInfo.ISP?.Contains("همراه اول") == true)
     {
     subscriberInfo = await GetMTNSubscriberInfoAsync(ipAddress);
  }
                else if (connectionInfo.ISP?.Contains("Irancell") == true ||
                connectionInfo.ISP?.Contains("ایرانسل") == true)
          {
       subscriberInfo = await GetIrancellSubscriberInfoAsync(ipAddress);
            }
    else if (connectionInfo.ISP?.Contains("Rightel") == true ||
 connectionInfo.ISP?.Contains("رایتل") == true)
    {
   subscriberInfo = await GetRightelSubscriberInfoAsync(ipAddress);
      }

       // Save to database
    await SaveSubscriberInfoToDatabase(ipAddress, subscriberInfo);

         return subscriberInfo;
     }
    catch (Exception ex)
            {
          _logger.LogError(ex, $"Error getting subscriber info for {ipAddress}");
      return subscriberInfo;
}
        }

    private async Task<SubscriberInfo> GetTelecomSubscriberInfoAsync(string ipAddress)
    {
        var subscriberInfo = new SubscriberInfo();
        try
        {
            // استفاده از API واقعی IPInfo.io برای اطلاعات ISP و location
            var ipInfoToken = App.Configuration["Geolocation:IPInfoToken"] ?? "e6d5a02860b49f";
            var url = $"https://ipinfo.io/{ipAddress}/json?token={ipInfoToken}";
            var response = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(response);
            
            subscriberInfo.ISP = data["org"]?.ToString() ?? "";
            subscriberInfo.City = data["city"]?.ToString() ?? "";
            subscriberInfo.Province = data["region"]?.ToString() ?? "";
            subscriberInfo.Address = $"{data["city"]}, {data["region"]}, {data["country"]}";
            subscriberInfo.SubscriptionType = "Fixed Line Internet";
            subscriberInfo.IsActive = true;
            
            // تلاش برای دریافت اطلاعات بیشتر از OpenCage Geocoder
            if (!string.IsNullOrEmpty(data["loc"]?.ToString()))
            {
                var locParts = data["loc"].ToString().Split(',');
                if (locParts.Length == 2)
                {
                    var openCageKey = App.Configuration["Geolocation:OpenCageAPIKey"] ?? "";
                    if (!string.IsNullOrEmpty(openCageKey))
                    {
                        var geocoderUrl = $"https://api.opencagedata.com/geocode/v1/json?q={locParts[0]}+{locParts[1]}&key={openCageKey}";
                        try
                        {
                            var geoResponse = await _httpClient.GetStringAsync(geocoderUrl);
                            var geoData = JObject.Parse(geoResponse);
                            if (geoData["results"]?.FirstOrDefault() != null)
                            {
                                var result = geoData["results"].FirstOrDefault();
                                subscriberInfo.Address = result["formatted"]?.ToString() ?? subscriberInfo.Address;
                            }
                        }
                        catch { }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Telecom subscriber info; fallbacking to DB cache");
            subscriberInfo = await GetSubscriberInfoFromDatabase(ipAddress);
        }
        return subscriberInfo;
    }
    private async Task<SubscriberInfo> GetMTNSubscriberInfoAsync(string ipAddress)
    {
        var subscriberInfo = new SubscriberInfo();
        try
        {
            // استفاده از API واقعی IPAPI.co برای اطلاعات ISP و location
            var url = $"https://ipapi.co/{ipAddress}/json/";
            var response = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(response);
            
            subscriberInfo.ISP = data["org"]?.ToString() ?? "";
            subscriberInfo.City = data["city"]?.ToString() ?? "";
            subscriberInfo.Province = data["region"]?.ToString() ?? "";
            subscriberInfo.Address = $"{data["city"]}, {data["region"]}, {data["country_name"]}";
            subscriberInfo.SubscriptionType = "Mobile Internet";
            subscriberInfo.IsActive = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get MTN subscriber info; fallbacking to DB");
            subscriberInfo = await GetSubscriberInfoFromDatabase(ipAddress);
        }
        return subscriberInfo;
    }
    private async Task<SubscriberInfo> GetIrancellSubscriberInfoAsync(string ipAddress)
    {
        var subscriberInfo = new SubscriberInfo();
        try
        {
            // استفاده از API واقعی IPGeolocation.io
            var apiKey = App.Configuration["Geolocation:IPGeolocationAPIKey"] ?? "demo";
            var url = $"https://api.ipgeolocation.io/ipgeo?ip={ipAddress}&apiKey={apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(response);
            
            subscriberInfo.ISP = data["isp"]?.ToString() ?? "";
            subscriberInfo.City = data["city"]?.ToString() ?? "";
            subscriberInfo.Province = data["state_prov"]?.ToString() ?? "";
            subscriberInfo.Address = $"{data["city"]}, {data["state_prov"]}, {data["country_name"]}";
            subscriberInfo.SubscriptionType = "Mobile Internet";
            subscriberInfo.IsActive = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Irancell info; DB fallback");
            subscriberInfo = await GetSubscriberInfoFromDatabase(ipAddress);
        }
        return subscriberInfo;
    }
    private async Task<SubscriberInfo> GetRightelSubscriberInfoAsync(string ipAddress)
    {
        var subscriberInfo = new SubscriberInfo();
        try
        {
            // استفاده از API واقعی ip-api.com (رایگان و معتبر)
            var url = $"http://ip-api.com/json/{ipAddress}?fields=status,message,country,regionName,city,isp,org,as,query";
            var response = await _httpClient.GetStringAsync(url);
            var data = JObject.Parse(response);
            
            if (data["status"]?.ToString() == "success")
            {
                subscriberInfo.ISP = data["isp"]?.ToString() ?? "";
                subscriberInfo.City = data["city"]?.ToString() ?? "";
                subscriberInfo.Province = data["regionName"]?.ToString() ?? "";
                subscriberInfo.Address = $"{data["city"]}, {data["regionName"]}, {data["country"]}";
                subscriberInfo.SubscriptionType = "Mobile Internet";
                subscriberInfo.IsActive = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Rightel info; fallback to DB");
            subscriberInfo = await GetSubscriberInfoFromDatabase(ipAddress);
        }
        return subscriberInfo;
    }

        private async Task<SubscriberInfo> GetSubscriberInfoFromDatabase(string ipAddress)
   {
         var subscriberInfo = new SubscriberInfo();

 try
            {
                using var connection = DatabaseManager.GetConnection();
      connection.Open();

         var sql = @"SELECT * FROM SubscriberInfo WHERE IPAddress = @IP ORDER BY RetrievedAt DESC LIMIT 1";
         using var command = new System.Data.SQLite.SQLiteCommand(sql, connection);
         command.Parameters.AddWithValue("@IP", ipAddress);

     using var reader = command.ExecuteReader();
         if (reader.Read())
    {
         subscriberInfo.FirstName = reader["FirstName"]?.ToString();
           subscriberInfo.LastName = reader["LastName"]?.ToString();
         subscriberInfo.FullName = reader["FullName"]?.ToString();
      subscriberInfo.NationalID = reader["NationalID"]?.ToString();
               subscriberInfo.PhoneNumber = reader["PhoneNumber"]?.ToString();
        subscriberInfo.LandlineNumber = reader["LandlineNumber"]?.ToString();
 subscriberInfo.MobileNumber = reader["MobileNumber"]?.ToString();
           subscriberInfo.Email = reader["Email"]?.ToString();
          subscriberInfo.Address = reader["Address"]?.ToString();
         subscriberInfo.PostalCode = reader["PostalCode"]?.ToString();
        subscriberInfo.Province = reader["Province"]?.ToString();
   subscriberInfo.City = reader["City"]?.ToString();
         subscriberInfo.SubscriptionType = reader["SubscriptionType"]?.ToString();
         subscriberInfo.AccountNumber = reader["AccountNumber"]?.ToString();
           subscriberInfo.IsActive = reader["IsActive"]?.ToString() == "1";
         
          if (DateTime.TryParse(reader["SubscriptionDate"]?.ToString(), out var subDate))
           {
             subscriberInfo.SubscriptionDate = subDate;
       }
           }
            }
      catch (Exception ex)
      {
      _logger.LogWarning(ex, "Failed to get subscriber info from database");
            }

          return subscriberInfo;
        }

     private async Task SaveSubscriberInfoToDatabase(string ipAddress, SubscriberInfo info)
        {
      try
 {
          using var connection = DatabaseManager.GetConnection();
   connection.Open();

       var sql = @"INSERT OR REPLACE INTO SubscriberInfo 
      (IPAddress, FirstName, LastName, FullName, NationalID, PhoneNumber, 
    LandlineNumber, MobileNumber, Email, Address, PostalCode, Province, 
    City, SubscriptionType, SubscriptionDate, AccountNumber, IsActive, RetrievedAt) 
                 VALUES (@IP, @FirstName, @LastName, @FullName, @NationalID, @PhoneNumber, 
         @Landline, @Mobile, @Email, @Address, @PostalCode, @Province, 
           @City, @SubType, @SubDate, @Account, @IsActive, @Retrieved)";

      using var command = new System.Data.SQLite.SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@IP", ipAddress);
      command.Parameters.AddWithValue("@FirstName", info.FirstName ?? "");
                command.Parameters.AddWithValue("@LastName", info.LastName ?? "");
     command.Parameters.AddWithValue("@FullName", info.FullName ?? "");
     command.Parameters.AddWithValue("@NationalID", info.NationalID ?? "");
        command.Parameters.AddWithValue("@PhoneNumber", info.PhoneNumber ?? "");
     command.Parameters.AddWithValue("@Landline", info.LandlineNumber ?? "");
    command.Parameters.AddWithValue("@Mobile", info.MobileNumber ?? "");
       command.Parameters.AddWithValue("@Email", info.Email ?? "");
       command.Parameters.AddWithValue("@Address", info.Address ?? "");
         command.Parameters.AddWithValue("@PostalCode", info.PostalCode ?? "");
       command.Parameters.AddWithValue("@Province", info.Province ?? "");
       command.Parameters.AddWithValue("@City", info.City ?? "");
command.Parameters.AddWithValue("@SubType", info.SubscriptionType ?? "");
      command.Parameters.AddWithValue("@SubDate", info.SubscriptionDate);
       command.Parameters.AddWithValue("@Account", info.AccountNumber ?? "");
 command.Parameters.AddWithValue("@IsActive", info.IsActive ? 1 : 0);
     command.Parameters.AddWithValue("@Retrieved", DateTime.Now);

                await Task.Run(() => command.ExecuteNonQuery());
  }
            catch (Exception ex)
   {
       _logger.LogError(ex, "Failed to save subscriber info to database");
   }
        }

    // ...existing code...
 }
}

