using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAwaraIT.Mapping
{
    /// <summary>
    /// Название логических имен для своих таблиц и столбцов, в случае если называются по иному, то нужно поменять соовтесующие поля
    /// </summary>

    public static class ColumnMap
    {
        public static class Opportunity
        {
            public const string EntityLogicalName = "opportunity";
            public const string Contact = "customerid";
            public const string Territory = "territoryid";
            public const string Name = "name";
        }
        public static class Course
        {
            public const string EntityLogicalName = "nk_nkcourses";
            public const string Name = "nk_name";
            public const string SubjectId = "nk_subjectid";
            public const string PreparationFormatId = "nk_preparationformatcourseid";
            public const string FormatId = "nk_formatcourseid";
            public const string TerritoryId = "nk_territoryid";
        }
        public static class Territory
        {
            public const string EntityLogicalName = "nk_nkcountries";
        }

        public static class Deal
        {
            public const string EntityLogicalName = "nk_nkpossibledeal";
            public const string Territory = "nk_territoryid";
            public const string ClientId = "nk_nkclientid";
            public const string StatusCode = "nk_nkstatuscode";
            public const string TerritoryId = "nk_territoryid";
            public const string OwnerId = "ownerid";
            public const string Name = "nk_name";
        }
        public static class PriceListPosition
        {
            public const string EntityLogicalName = "nk_pricelistposition";
            public const string PreparationFormatId = "nk_preparationformatid";
            public const string ConductFormatId = "nk_conductformatid";
            public const string TerritoryId = "nk_territoryid";
            public const string Price = "nk_price";
        }
        public static class Interest
        {
            public const string EntityLogicalName = "nk_nkinterest";
            public const string Email = "nk_email";
            public const string Phone = "nk_phonenumber";
            public const string FirstName = "nk_firstname";
            public const string LastName = "nk_lastname";
            public const string CountryId = "nk_countryid";
            public const string ClientId = "nk_clientid";
            public const string OwnerId = "ownerid";
            public const string StatusCode = "nk_statuscode";
        }

        public static class ClientCard
        {
            public const string EntityLogicalName = "nk_nkclientcards";
            public const string Id = "nk_nkclientcardsid";
            public const string FirstName = "nk_firstname";
            public const string LastName = "nk_lastname";
            public const string Email = "nk_email";
            public const string Phone = "nk_phonenumber";
            public const string Name = "nk_name";
            public const string CountryId = "nk_countryid";
        }

        public static class Team
        {
            public const string EntityLogicalName = "teammembership";
            public const string UserId = "systemuserid";
        }

        public static class SystemUser
        {
            public const string EntityLogicalName = "systemuser";
            public const string IsDisabled = "isdisabled";
        }
    }

}
