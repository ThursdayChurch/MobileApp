﻿using System;
using Rock.Mobile.Network;
using System.Net;
using System.Collections.Generic;
using Rock.Mobile;
using App.Shared.Network;
using App.Shared.PrivateConfig;

namespace MobileApp
{
    /// <summary>
    /// Implements api methods that are specific to MobileApp.
    /// </summary>
    public static class MobileAppApi
    {
        public static void GetNews( HttpRequest.RequestResult< List<Rock.Client.ContentChannelItem> > resultHandler )
        {
            string oDataFilter = "";

            // if they're a developer, pull down pending and future start date items as well.
            if( RockGeneralData.Instance.Data.DeveloperModeEnabled == true )
            {
                oDataFilter = string.Format( "?$filter=ContentChannel/Guid eq guid'EAE51F3E-C27B-4E7C-B9A0-16EB68129637' and " +
                    "(Status eq '2' or Status eq '1') and " +
                    "(ExpireDateTime ge DateTime'{0}' or ExpireDateTime eq null)&LoadAttributes=True", 
                    DateTime.Now.ToString( "s" ) );
            }
            else
            {
                oDataFilter = string.Format( "?$filter=ContentChannel/Guid eq guid'EAE51F3E-C27B-4E7C-B9A0-16EB68129637' and " +
                    "Status eq '2' and (StartDateTime le DateTime'{0}' or StartDateTime eq null) and " +
                    "(ExpireDateTime ge DateTime'{0}' or ExpireDateTime eq null)&LoadAttributes=True", 
                    DateTime.Now.ToString( "s" ) );
            }

            RockApi.Get_ContentChannelItems( oDataFilter, resultHandler );
        }

        class DateTimeModel
        {
            public string ValueAsDateTime { get; set; }
        }

        const int GeneralDataTimeValueId = 2623;
        public static void GetGeneralDataTime( HttpRequest.RequestResult<DateTime> resultHandler )
        {
            string oDataFilter = string.Format( "?$filter=AttributeId eq {0}", GeneralDataTimeValueId );
            RockApi.Get_AttributeValues<List<DateTimeModel>>( oDataFilter,
                delegate(HttpStatusCode statusCode, string statusDescription, List<DateTimeModel> dateTimeList) 
                {
                    DateTime dateTime = DateTime.MinValue;
                    if( dateTimeList != null && dateTimeList.Count > 0 && dateTimeList[ 0 ].ValueAsDateTime != null )
                    {
                        dateTime = DateTime.Parse( dateTimeList[ 0 ].ValueAsDateTime );
                    }

                    resultHandler( statusCode, statusDescription, dateTime );
                } );
        }

        public static void GetPrayerCategories( HttpRequest.RequestResult<List<Rock.Client.Category>> resultHandler )
        {
            RockApi.Get_Categories_GetChildren_1( resultHandler );
        }

        public static void GetPublicGroupsByLocation( int geoFenceGroupTypeId, int groupTypeId, int locationId, HttpRequest.RequestResult< List<Rock.Client.Group> > resultHandler )
        {
            string oDataFilter = "?$filter=IsPublic eq true";
            RockApi.Get_Groups_ByLocation( geoFenceGroupTypeId, groupTypeId, locationId, oDataFilter, resultHandler );
        }

        const int GroupRegistrationValueId = 52;
        public static void JoinGroup( Rock.Client.Person person, string firstName, string lastName, string spouseName, string email, string phone, int groupId, string groupName, HttpRequest.RequestResult resultHandler )
        {
            if ( person.PrimaryAliasId.HasValue && person.PrimaryAliasId.Value != 0 )
            {
                // resolve the alias ID
                ApplicationApi.ResolvePersonAliasId( person, 
                    delegate(int personId )
                    {
                        string oDataFilter = string.Format( "/{0}?PersonAliasId={1}&FirstName={2}&LastName={3}&SpouseName={4}&Email={5}&MobilePhone={6}&GroupId={7}&GroupName={8}", 
                            GroupRegistrationValueId, personId, firstName, lastName, spouseName, email, phone, groupId, groupName );

                        RockApi.Post_Workflows_WorkflowEntry( oDataFilter, resultHandler );
                    } );
            }
            else
            {
                // no ID, so just send the info
                string oDataFilter = string.Format( "/{0}?PersonAliasId={1}&FirstName={2}&LastName={3}&SpouseName={4}&Email={5}&MobilePhone={6}&GroupId={7}&GroupName={8}", 
                    GroupRegistrationValueId, 0, firstName, lastName, spouseName, email, phone, groupId, groupName );

                RockApi.Post_Workflows_WorkflowEntry( oDataFilter, resultHandler );
            }
        }

        public delegate void OnFamilyAndAddressResult( System.Net.HttpStatusCode code, string desc, Rock.Client.Group family, Rock.Client.GroupLocation familyAddress );
        public static void GetFamilyAndAddress( Rock.Client.Person person, OnFamilyAndAddressResult resultHandler )
        {
            // for the address (which implicitly is their primary residence address), first get all group locations associated with them
            ApplicationApi.GetFamiliesOfPerson( person, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Group> model)
                {
                    Rock.Client.Group family = null;
                    Rock.Client.GroupLocation familyAddress = null;
                    
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        // find what we'll consider their primary address.

                        // look thru each group (family)
                        foreach( Rock.Client.Group personGroup in model )
                        {
                            // If we find a groupType of family, that should be their primary family.
                            if( personGroup.GroupType.Guid.ToString( ).ToLower( ) == Rock.Client.SystemGuid.GroupType.GROUPTYPE_FAMILY.ToLower( ) )
                            {
                                // store the family
                                family = personGroup;

                                // look at each location within the family
                                foreach( Rock.Client.GroupLocation groupLocation in family.GroupLocations )
                                {
                                    // find their "Home Location" within the family group type.
                                    if( groupLocation.GroupLocationTypeValue.Guid.ToString( ).ToLower( ) == Rock.Client.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.ToLower( ) )
                                    {
                                        familyAddress = groupLocation;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // notify the caller
                    resultHandler( statusCode, statusDescription, family, familyAddress );
                });
        }

        /// <summary>
        /// Returns the phone number matching phoneTypeId, or an empty one if no match is found.
        /// </summary>
        /// <returns>The phone number.</returns>
        /// <param name="phoneTypeId">Phone type identifier.</param>
        static Rock.Client.PhoneNumber TryGetPhoneNumber( int phoneTypeId, ICollection<Rock.Client.PhoneNumber> phoneNumbers )
        {
            Rock.Client.PhoneNumber requestedNumber = null;

            // if the user has phone numbers
            if ( phoneNumbers != null )
            {
                // get an enumerator
                IEnumerator<Rock.Client.PhoneNumber> enumerator = phoneNumbers.GetEnumerator( );
                enumerator.MoveNext( );

                // search for the phone number type requested
                while ( enumerator.Current != null )
                {
                    Rock.Client.PhoneNumber phoneNumber = enumerator.Current as Rock.Client.PhoneNumber;

                    // is this the right type?
                    if ( phoneNumber.NumberTypeValueId == phoneTypeId )
                    {
                        requestedNumber = phoneNumber;
                        break;
                    }
                    enumerator.MoveNext( );
                }
            }

            return requestedNumber;
        }

        public delegate void OnProfileAndCellPhoneResult( System.Net.HttpStatusCode code, string desc, Rock.Client.Person person, Rock.Client.PhoneNumber phoneNumber );
        public static void GetProfileAndCellPhone( string userID, OnProfileAndCellPhoneResult resultHandler )
        {
            RockApi.Get_People_ByUserName( userID, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription, Rock.Client.Person model)
                {
                    Rock.Client.PhoneNumber cellPhoneNumber = null;

                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true && model != null )
                    {
                        // search for a phone number (which should match whatever we already have, unless this is a new login)
                        cellPhoneNumber = TryGetPhoneNumber( PrivateGeneralConfig.CellPhoneValueId, model.PhoneNumbers );

                        // now before storing the person, clear their phone list, as we need to store and manage
                        // the number seperately.
                        model.PhoneNumbers = null;
                    }

                    // notify the caller
                    resultHandler( statusCode, statusDescription, model, cellPhoneNumber );
                });
        }

        public static void UpdateOrAddPhoneNumber( Rock.Client.Person person, Rock.Client.PhoneNumber phoneNumber, bool isNew, HttpRequest.RequestResult<Rock.Client.PhoneNumber> resultHandler )
        {
            // is it blank?
            if ( string.IsNullOrEmpty( phoneNumber.Number ) == true )
            {
                // if it's not new, we should delete an existing
                if ( isNew == false )
                {
                    ApplicationApi.DeleteCellPhoneNumber( phoneNumber, 
                        delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                        {
                            if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                            {
                                // return the blank number back to them
                                resultHandler( statusCode, statusDescription, phoneNumber );
                            }
                        } );
                }
                // otherwise, simply ignore it and say we're done.
                else
                {
                    resultHandler( System.Net.HttpStatusCode.OK, "", phoneNumber );
                }
            }
            // not blank, so we're adding or updating
            else
            {
                // send it to the server
                ApplicationApi.AddOrUpdateCellPhoneNumber( person, phoneNumber, isNew, 0, 
                    delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                    {
                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                        {
                            // if it was new, get it so we have its ID
                            if( isNew == true )
                            {
                                ApplicationApi.GetPhoneNumberByGuid( phoneNumber.Guid, resultHandler );
                            }
                            else
                            {
                                // it's updated, so now just return what we updated.
                                resultHandler( statusCode, statusDescription, phoneNumber );
                            }
                        }
                        // something went wrong, so return.
                        else
                        {
                            resultHandler( statusCode, statusDescription, null );
                        }
                    } );
            }
        }

        public delegate void ImpersonationTokenResponse( string impersonationToken );
        public static void TryGetImpersonationToken( ImpersonationTokenResponse response )
        {
            // if the user is logged in and has an alias ID, try to get it
            if ( App.Shared.Network.RockMobileUser.Instance.LoggedIn == true && App.Shared.Network.RockMobileUser.Instance.Person.PrimaryAliasId.HasValue == true )
            {
                // make the request
                ApplicationApi.GetImpersonationToken( App.Shared.Network.RockMobileUser.Instance.Person.PrimaryAliasId.Value, 
                    delegate(System.Net.HttpStatusCode statusCode, string statusDescription, string impersonationToken )
                    {
                        // whether it succeeded or not, hand them the response
                        response( impersonationToken );
                    } );
            }
            else
            {
                // they didn't pass requirements, so hand back an empty string.
                response( string.Empty );
            }
        }

        const string RegisterResult_BadLogin = "CreateLoginError";
        const int UserLoginEntityTypeId = 27;
        public static void RegisterNewUser( Rock.Client.Person person, Rock.Client.PhoneNumber phoneNumber, string username, string password, HttpRequest.RequestResult resultHandler )
        {
            //JHM 3-11-15 ALMOST FRIDAY THE 13th SIX YEARS LATER YAAAHH!!!!!
            //Until we release, or at least are nearly done testing, do not allow actual user registrations.
            //resultHandler( HttpStatusCode.OK, "" );
            //return;

            // this is a complex end point. To register a user is a multiple step process.
            //1. Create a new Person on Rock
            //2. Request that Person back
            //3. Create a new Login for that person
            //4. Post the location, phone number and home campus
            person.Guid = Guid.NewGuid( );

            RockApi.Post_People( person, 0, 
                delegate(System.Net.HttpStatusCode statusCode, string statusDescription )
                {
                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                    {
                        ApplicationApi.GetPersonByGuid( person.Guid,
                            delegate(System.Net.HttpStatusCode personStatusCode, string personStatusDescription, Rock.Client.Person createdPerson ) 
                            {
                                if ( Rock.Mobile.Network.Util.StatusInSuccessRange( personStatusCode ) )
                                {
                                    // it worked. now put their login info.
                                    RockApi.Post_UserLogins( createdPerson.Id, username, password, UserLoginEntityTypeId,
                                        delegate(System.Net.HttpStatusCode loginStatusCode, string loginStatusDescription) 
                                        {
                                            // if this worked, we are home free
                                            if ( Rock.Mobile.Network.Util.StatusInSuccessRange( loginStatusCode ) )
                                            {
                                                // now update their phone number, if valid
                                                if( phoneNumber != null )
                                                {
                                                    ApplicationApi.AddOrUpdateCellPhoneNumber( createdPerson, phoneNumber, true, 0, 
                                                        delegate(HttpStatusCode phoneStatusCode, string phoneStatusDescription) 
                                                        {
                                                            // NOTICE: We are passing back the loginStatus, not the phone status.
                                                            // This is because if the login was created, we're DONE and they can login.
                                                            // Updating their phone number is purely a bonus.
                                                            if( resultHandler != null )
                                                            {
                                                                resultHandler( loginStatusCode, loginStatusDescription );
                                                            }
                                                        });
                                                }
                                                else
                                                {
                                                    // phone number not provided, go with the result of the login creation
                                                    if( resultHandler != null )
                                                    {
                                                        resultHandler( loginStatusCode, RegisterResult_BadLogin );
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // Failed
                                                if( resultHandler != null )
                                                {
                                                    resultHandler( loginStatusCode, RegisterResult_BadLogin );
                                                }
                                            }
                                        });
                                }
                                else
                                {
                                    // Failed
                                    if( resultHandler != null )
                                    {
                                        resultHandler( personStatusCode, personStatusDescription );
                                    }
                                }
                            });
                    }
                    else
                    {
                        // Failed
                        if( resultHandler != null )
                        {
                            resultHandler( statusCode, statusDescription );
                        }
                    }
                } );
        }
    }
}
