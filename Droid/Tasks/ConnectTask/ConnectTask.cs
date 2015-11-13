﻿using System;
using Android.App;
using Android.Views;
using App.Shared.Strings;
using App.Shared;

namespace Droid
{
    namespace Tasks
    {
        namespace Connect
        {
            public class ConnectTask : Task
            {
                ConnectPrimaryFragment MainPage { get; set; }
                GroupFinderFragment GroupFinder { get; set; }
                JoinGroupFragment JoinGroup { get; set; }
                TaskWebFragment WebFragment { get; set; }

                public ConnectTask( NavbarFragment navFragment ) : base( navFragment )
                {
                    // create our fragments (which are basically equivalent to iOS ViewControllers)
                    MainPage = navFragment.FragmentManager.FindFragmentByTag( "Droid.ConnectPrimaryFragment" ) as ConnectPrimaryFragment;
                    if ( MainPage == null )
                    {
                        MainPage = new ConnectPrimaryFragment();
                    }
                    MainPage.ParentTask = this;

                    GroupFinder = navFragment.FragmentManager.FindFragmentByTag( "Droid.GroupFinderFragment" ) as GroupFinderFragment;
                    if ( GroupFinder == null )
                    {
                        GroupFinder = new GroupFinderFragment();
                    }
                    GroupFinder.ParentTask = this;

                    JoinGroup = navFragment.FragmentManager.FindFragmentByTag( "Droid.JoinGroupFragment" ) as JoinGroupFragment;
                    if ( JoinGroup == null )
                    {
                        JoinGroup = new JoinGroupFragment();
                    }
                    JoinGroup.ParentTask = this;

                    WebFragment = new TaskWebFragment( );
                    WebFragment.ParentTask = this;
                }

                public override TaskFragment StartingFragment()
                {
                    return MainPage;
                }

                public override void OnUp( MotionEvent e )
                {
                    base.OnUp( e );
                }

                public override void OnClick(Fragment source, int buttonId, object context)
                {
                    base.OnClick(source, buttonId, context);

                    // only handle input if the springboard is closed
                    if ( NavbarFragment.ShouldTaskAllowInput( ) )
                    {
                        // decide what to do.
                        if ( source == MainPage )
                        {
                            ConnectLink linkEntry = (ConnectLink)context;

                            // group finder is the only connect link that doesn't use an embedded webView.
                            if ( linkEntry.Title == ConnectStrings.Main_Connect_GroupFinder )
                            {
                                // launch group finder (and have it auto-show the search)
                                GroupFinder.ShowSearchOnAppear = true;

                                // if we're logged in, give it a starting address
                                if ( App.Shared.Network.RockMobileUser.Instance.LoggedIn == true && App.Shared.Network.RockMobileUser.Instance.HasFullAddress( ) )
                                {
                                    GroupFinder.SetSearchAddress( App.Shared.Network.RockMobileUser.Instance.Street1( ), 
                                        App.Shared.Network.RockMobileUser.Instance.City( ),
                                        App.Shared.Network.RockMobileUser.Instance.State( ),
                                        App.Shared.Network.RockMobileUser.Instance.Zip( ) );
                                }

                                PresentFragment( GroupFinder, true );
                            }
                            else
                            {
                                // launch the ConnectWebFragment.
                                TaskWebFragment.HandleUrl( false, true, linkEntry.Url, this, WebFragment );
                            }
                        }
                        else if ( source == GroupFinder )
                        {
                            // turn off auto-show search so that if the user presses 'back', we don't pop it up again.
                            GroupFinder.ShowSearchOnAppear = false;
                            
                            App.Shared.GroupFinder.GroupEntry entry = (App.Shared.GroupFinder.GroupEntry)context;

                            JoinGroup.GroupTitle = entry.Title;
                            JoinGroup.Distance = string.Format( "{0:##.0} {1}", entry.Distance, ConnectStrings.GroupFinder_MilesSuffix );
                            JoinGroup.GroupID = entry.Id;
                            JoinGroup.MeetingTime = string.IsNullOrEmpty( entry.MeetingTime) == false ? entry.MeetingTime : ConnectStrings.GroupFinder_ContactForTime;

                            PresentFragment( JoinGroup, true );
                        }
                    }
                }
            }
        }
    }
}

