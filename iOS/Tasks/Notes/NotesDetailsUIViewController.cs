using System;
using Foundation;
using UIKit;
using System.CodeDom.Compiler;
using App.Shared.Notes.Model;
using System.Collections.Generic;
using CoreGraphics;
using App.Shared;
using App.Shared.Config;
using Rock.Mobile.UI;
using System.IO;
using App.Shared.Analytics;
using App.Shared.Network;
using App.Shared.PrivateConfig;
using Rock.Mobile.IO;
using App.Shared.Strings;

namespace iOS
{
    partial class NotesDetailsUIViewController : TaskUIViewController
	{
        public class TableViewDelegate : NavBarRevealHelperDelegate
        {
            TableSource TableSource { get; set; }

            public TableViewDelegate( TableSource tableSource, NavToolbar toolbar ) : base( toolbar )
            {
                TableSource = tableSource;
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                return TableSource.GetHeightForRow( tableView, indexPath );
            }

            public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
            {
                TableSource.RowSelected( tableView, indexPath );
            }
        }

        public class TableSource : UITableViewSource 
        {
            /// <summary>
            /// Definition for the primary (top) cell, which advertises the current series
            /// more prominently
            /// </summary>
            class SeriesPrimaryCell : UITableViewCell
            {
                public static string Identifier = "SeriesPrimaryCell";

                public TableSource Parent { get; set; }

                public UIImageView Image { get; set; }

                public UILabel Title { get; set; }
                public UITextView Desc { get; set; }
                public UILabel Date { get; set; }

                public SeriesPrimaryCell( UITableViewCellStyle style, string cellIdentifier ) : base( style, cellIdentifier )
                {
                    BackgroundColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.BG_Layer_Color );

                    // anything that's constant can be set here once in the constructor
                    Image = new UIImageView( );
                    Image.ContentMode = UIViewContentMode.ScaleAspectFit;
                    Image.Layer.AnchorPoint = CGPoint.Empty;
                    AddSubview( Image );

                    Title = new UILabel( );
                    Title.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( ControlStylingConfig.Font_Bold, ControlStylingConfig.Large_FontSize );
                    Title.Layer.AnchorPoint = CGPoint.Empty;
                    Title.TextColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.Label_TextColor );
                    Title.BackgroundColor = UIColor.Clear;
                    Title.LineBreakMode = UILineBreakMode.TailTruncation;
                    AddSubview( Title );

                    Date = new UILabel( );
                    Date.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( ControlStylingConfig.Font_Regular, ControlStylingConfig.Small_FontSize );
                    Date.Layer.AnchorPoint = CGPoint.Empty;
                    Date.TextColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.TextField_PlaceholderTextColor );
                    Date.BackgroundColor = UIColor.Clear;
                    Date.LineBreakMode = UILineBreakMode.TailTruncation;
                    AddSubview( Date );

                    Desc = new UITextView( );
                    Desc.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( ControlStylingConfig.Font_Light, ControlStylingConfig.Small_FontSize );
                    Desc.Layer.AnchorPoint = CGPoint.Empty;
                    Desc.TextColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.Label_TextColor );
                    Desc.BackgroundColor = UIColor.Clear;
                    Desc.TextContainerInset = UIEdgeInsets.Zero;
                    Desc.TextContainer.LineFragmentPadding = 0;
                    Desc.Editable = false;
                    Desc.UserInteractionEnabled = false;
                    AddSubview( Desc );
                }
            }

            /// <summary>
            /// Definition for each cell in this table
            /// </summary>
            class SeriesCell : UITableViewCell
            {
                public static string Identifier = "SeriesCell";

                public TableSource Parent { get; set; }

                public UILabel Title { get; set; }
                public UILabel Date { get; set; }
                public UILabel Speaker { get; set; }
                public UIButton PodcastButton { get; set; }
                public UIButton TakeNotesButton { get; set; }
                public UIButton ShareButton { get; set; }

                public UIView Seperator { get; set; }

                public int RowIndex { get; set; }

                public SeriesCell( UITableViewCellStyle style, string cellIdentifier ) : base( style, cellIdentifier )
                {
                    Title = new UILabel( );
                    Title.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( ControlStylingConfig.Font_Bold, ControlStylingConfig.Medium_FontSize );

                    Title.Layer.AnchorPoint = CGPoint.Empty;
                    Title.TextColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.Label_TextColor );
                    Title.BackgroundColor = UIColor.Clear;
                    Title.LineBreakMode = UILineBreakMode.TailTruncation;
                    AddSubview( Title );

                    Date = new UILabel( );
                    Date.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( ControlStylingConfig.Font_Regular, ControlStylingConfig.Small_FontSize );
                    Date.Layer.AnchorPoint = CGPoint.Empty;
                    Date.TextColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.TextField_PlaceholderTextColor );
                    Date.BackgroundColor = UIColor.Clear;
                    Date.LineBreakMode = UILineBreakMode.TailTruncation;
                    AddSubview( Date );

                    Speaker = new UILabel( );
                    Speaker.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( ControlStylingConfig.Font_Regular, ControlStylingConfig.Small_FontSize );
                    Speaker.Layer.AnchorPoint = CGPoint.Empty;
                    Speaker.TextColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.TextField_PlaceholderTextColor );
                    Speaker.BackgroundColor = UIColor.Clear;
                    Speaker.LineBreakMode = UILineBreakMode.TailTruncation;
                    AddSubview( Speaker );

                    PodcastButton = new UIButton( UIButtonType.Custom );
                    PodcastButton.TouchUpInside += (object sender, EventArgs e) => { Parent.RowButtonClicked( RowIndex, 0 ); };
                    PodcastButton.Layer.AnchorPoint = CGPoint.Empty;
                    PodcastButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( PrivateControlStylingConfig.Icon_Font_Secondary, PrivateNoteConfig.Details_Table_IconSize );
                    PodcastButton.SetTitle( PrivateNoteConfig.Series_Table_Podcast_Icon, UIControlState.Normal );
                    PodcastButton.BackgroundColor = UIColor.Clear;
                    PodcastButton.SizeToFit( );
                    AddSubview( PodcastButton );

                    TakeNotesButton = new UIButton( UIButtonType.Custom );
                    TakeNotesButton.TouchUpInside += (object sender, EventArgs e) => { Parent.RowButtonClicked( RowIndex, 1 ); };
                    TakeNotesButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( PrivateControlStylingConfig.Icon_Font_Secondary, PrivateNoteConfig.Details_Table_IconSize );
                    TakeNotesButton.SetTitle( PrivateNoteConfig.Series_Table_TakeNotes_Icon, UIControlState.Normal );
                    TakeNotesButton.Layer.AnchorPoint = CGPoint.Empty;
                    TakeNotesButton.BackgroundColor = UIColor.Clear;
                    TakeNotesButton.SizeToFit( );
                    AddSubview( TakeNotesButton );

                    ShareButton = new UIButton( UIButtonType.Custom );
                    ShareButton.TouchUpInside += ( object sender, EventArgs e ) => { Parent.RowButtonClicked( RowIndex, 2 ); };
                    ShareButton.Layer.AnchorPoint = CGPoint.Empty;
                    ShareButton.Font = Rock.Mobile.PlatformSpecific.iOS.Graphics.FontManager.GetFont( PrivateControlStylingConfig.Icon_Font_Secondary, PrivateNoteConfig.Details_Table_IconSize );
                    ShareButton.SetTitle( PrivateNoteConfig.Series_Table_Share_Icon, UIControlState.Normal );
                    ShareButton.BackgroundColor = UIColor.Clear;
                    ShareButton.SizeToFit();
                    AddSubview( ShareButton );

                    Seperator = new UIView( );
                    AddSubview( Seperator );
                    Seperator.Layer.BorderWidth = 1;
                    Seperator.Layer.BorderColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.BG_Layer_Color ).CGColor;
                }

                public void HideControls( bool hidden )
                {
                    // used for the final "dummy" row
                    Title.Hidden = hidden;
                    Date.Hidden = hidden;
                    Speaker.Hidden = hidden;
                    PodcastButton.Hidden = hidden;
                    TakeNotesButton.Hidden = hidden;
                    ShareButton.Hidden = hidden;
                }

                public void TogglePodcastButton( bool enabled )
                {
                    if ( enabled == true )
                    {
                        PodcastButton.Enabled = true;
                        PodcastButton.SetTitleColor( Rock.Mobile.UI.Util.GetUIColor( NoteConfig.Details_Table_IconColor ), UIControlState.Normal );
                    }
                    else
                    {
                        PodcastButton.Enabled = false;
                        PodcastButton.SetTitleColor( UIColor.DarkGray, UIControlState.Normal );
                    }
                }

                public void ToggleTakeNotesButton( bool enabled )
                {
                    if ( enabled == true )
                    {
                        TakeNotesButton.Enabled = true;
                        TakeNotesButton.SetTitleColor( Rock.Mobile.UI.Util.GetUIColor( NoteConfig.Details_Table_IconColor ), UIControlState.Normal );
                    }
                    else
                    {
                        TakeNotesButton.Enabled = false;
                        TakeNotesButton.SetTitleColor( UIColor.DarkGray, UIControlState.Normal );
                    }
                }

                public void ToggleShareButton( bool enabled )
                {
                    if ( enabled == true )
                    {
                        ShareButton.Enabled = true;
                        ShareButton.SetTitleColor( Rock.Mobile.UI.Util.GetUIColor( NoteConfig.Details_Table_IconColor ), UIControlState.Normal );
                    }
                    else
                    {
                        ShareButton.Enabled = false;
                        ShareButton.SetTitleColor( UIColor.DarkGray, UIControlState.Normal );
                    }
                }
            }

            NotesDetailsUIViewController Parent { get; set; }
            List<MessageEntry> MessageEntries { get; set; }
            Series Series { get; set; }

            nfloat PendingPrimaryCellHeight { get; set; }
            nfloat PendingCellHeight { get; set; }

            public TableSource (NotesDetailsUIViewController parent, List<MessageEntry> messages, Series series )
            {
                Parent = parent;
                MessageEntries = messages;
                Series = series;
            }

            public override nint RowsInSection (UITableView tableview, nint section)
            {
                return MessageEntries.Count + 2;
            }

            public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
            {
                tableView.DeselectRow( indexPath, true );
            }

            public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
            {
                return GetCachedRowHeight( tableView, indexPath );
            }

            public override nfloat EstimatedHeight(UITableView tableView, NSIndexPath indexPath)
            {
                return GetCachedRowHeight( tableView, indexPath );
            }

            nfloat GetCachedRowHeight( UITableView tableView, NSIndexPath indexPath )
            {
                // Depending on the row, we either want the primary cell's height,
                // or a standard row's height.
                switch ( indexPath.Row )
                {
                    case 0:
                    {
                        if ( PendingPrimaryCellHeight > 0 )
                        {
                            return PendingPrimaryCellHeight;
                        }
                        break;
                    }

                    default:
                    {
                        return PrivateNoteConfig.Series_Main_CellHeight;
                    }
                }

                // If we don't have the cell's height yet (first render), return the table's height
                return tableView.Frame.Height;
            }

            public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath)
            {
                if ( indexPath.Row == 0 )
                {
                    return GetPrimaryCell( tableView );
                }
                else
                {
                    return GetStandardCell( tableView, indexPath.Row - 1 );
                }
            }

            UITableViewCell GetPrimaryCell( UITableView tableView )
            {
                SeriesPrimaryCell cell = tableView.DequeueReusableCell( SeriesPrimaryCell.Identifier ) as SeriesPrimaryCell;

                // if there are no cells to reuse, create a new one
                if (cell == null)
                {
                    cell = new SeriesPrimaryCell( UITableViewCellStyle.Default, SeriesCell.Identifier );
                    cell.Parent = this;

                    // take the parent table's width so we inherit its width constraint
                    cell.Bounds = new CGRect( cell.Bounds.X, cell.Bounds.Y, tableView.Bounds.Width, cell.Bounds.Height );

                    // configure the cell colors
                    cell.BackgroundColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.BG_Layer_Color );
                    cell.SelectionStyle = UITableViewCellSelectionStyle.None;
                }

                // Banner Image
                cell.Image.Image = Parent.SeriesBillboard;
                cell.Image.SizeToFit( );

                // resize the image to fit the width of the device
                float imageAspect = (float) (cell.Image.Bounds.Height / cell.Image.Bounds.Width);
                cell.Image.Frame = new CGRect( 0, 0, cell.Bounds.Width, cell.Bounds.Width * imageAspect );

                // Title
                cell.Title.Text = Series.Name;
                if ( Series.Private == true )
                {
                    cell.Title.Text += " (Private)";
                }
                cell.Title.SizeToFit( );

                cell.Desc.Text = Series.Description;
                cell.Desc.Bounds = new CGRect( 0, 0, cell.Frame.Width - 20, float.MaxValue );
                cell.Desc.SizeToFit( );

                cell.Date.Text = Series.DateRanges;
                cell.Date.SizeToFit( );


                // now position the 3 text elements
                // Title
                cell.Title.Frame = new CGRect( 10, cell.Image.Frame.Bottom + 5, cell.Frame.Width - 20, cell.Title.Frame.Height );

                // Date
                cell.Date.Frame = new CGRect( 10, cell.Title.Frame.Bottom - 9, cell.Frame.Width - 20, cell.Date.Frame.Height + 5 );

                // Description
                cell.Desc.Frame = new CGRect( 10, cell.Date.Frame.Bottom + 5, cell.Frame.Width - 20, cell.Desc.Frame.Height + 5 );

                PendingPrimaryCellHeight = cell.Desc.Frame.Bottom + 5;

                return cell;
            }

            UITableViewCell GetStandardCell( UITableView tableView, int row )
            {
                SeriesCell cell = tableView.DequeueReusableCell( SeriesCell.Identifier ) as SeriesCell;

                // if there are no cells to reuse, create a new one
                if ( cell == null )
                {
                    cell = new SeriesCell( UITableViewCellStyle.Default, SeriesCell.Identifier );
                    cell.Parent = this;

                    // take the parent table's width so we inherit its width constraint
                    cell.Bounds = new CGRect( cell.Bounds.X, cell.Bounds.Y, tableView.Bounds.Width, cell.Bounds.Height );

                    // configure the cell colors
                    cell.BackgroundColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.BackgroundColor );
                    cell.SelectionStyle = UITableViewCellSelectionStyle.None;
                }

                if ( row < Series.Messages.Count )
                {
                    cell.HideControls( false );

                    // update the cell's row index so on button taps we know which one was tapped
                    cell.RowIndex = row;


                    // Create the title
                    cell.Title.Text = Series.Messages[ row ].Name;
                    if ( Series.Private == true ||
                         Series.Messages[ row ].Private == true )
                    {
                        cell.Title.Text += " (Private)";
                    }

                    cell.Title.SizeToFit( );

                    // Date
                    cell.Date.Text = Series.Messages[ row ].Date;
                    cell.Date.SizeToFit( );

                    // Speaker
                    cell.Speaker.Text = Series.Messages[ row ].Speaker;
                    cell.Speaker.SizeToFit( );


                    nfloat rowHeight = PrivateNoteConfig.Series_Main_CellHeight;
                    nfloat availableWidth = cell.Bounds.Width - cell.PodcastButton.Bounds.Width - cell.TakeNotesButton.Bounds.Width - cell.ShareButton.Bounds.Width;

                    // Position the Title & Date in the center to the right of the image
                    nfloat totalTextHeight = ( cell.Title.Bounds.Height + cell.Date.Bounds.Height + cell.Speaker.Bounds.Height ) - 6;

                    cell.Title.Frame = new CGRect( 10, ( ( rowHeight - totalTextHeight ) / 2 ) - 1, availableWidth, cell.Title.Frame.Height );
                    //cell.Title.BackgroundColor = UIColor.Blue;

                    cell.Date.Frame = new CGRect( cell.Title.Frame.Left, cell.Title.Frame.Bottom - 3, availableWidth, cell.Date.Frame.Height );
                    //cell.Date.BackgroundColor = UIColor.Yellow;

                    cell.Speaker.Frame = new CGRect( cell.Title.Frame.Left, cell.Date.Frame.Bottom - 3, availableWidth, cell.Speaker.Frame.Height );
                    //cell.Speaker.BackgroundColor = UIColor.Green;

                    // add the seperator to the bottom
                    cell.Seperator.Frame = new CGRect( 0, rowHeight - 1, cell.Bounds.Width, 1 );
                    //cell.Seperator.Hidden = true;

                    /*unchecked
                    {
                        cell.BackgroundColor = Rock.Mobile.UI.Util.GetUIColor( (uint)(0xFF0000FF - (row * 100)) );
                    }*/


                    // Buttons
                    cell.ShareButton.Frame = new CGRect( cell.Bounds.Width - cell.ShareButton.Bounds.Width,
                        ( rowHeight - cell.ShareButton.Bounds.Height ) / 2,
                        cell.ShareButton.Bounds.Width,
                        cell.ShareButton.Bounds.Height );

                    cell.TakeNotesButton.Frame = new CGRect( cell.ShareButton.Frame.Left - cell.TakeNotesButton.Bounds.Width,
                        ( rowHeight - cell.TakeNotesButton.Bounds.Height ) / 2,
                        cell.TakeNotesButton.Bounds.Width,
                        cell.TakeNotesButton.Bounds.Height );

                    cell.PodcastButton.Frame = new CGRect( cell.TakeNotesButton.Frame.Left - cell.PodcastButton.Bounds.Width,
                       ( rowHeight - cell.PodcastButton.Bounds.Height ) / 2,
                       cell.PodcastButton.Bounds.Width,
                       cell.PodcastButton.Bounds.Height );

                   
                    // disable the button if there's no listen URL
                    if ( string.IsNullOrEmpty( Series.Messages[ row ].AudioUrl ) &&
                        string.IsNullOrEmpty( Series.Messages[ row ].WatchUrl ) )
                    {
                        cell.TogglePodcastButton( false );
                    }
                    else
                    {
                        cell.TogglePodcastButton( true );
                    }

                    // disable the button if there's no note URL
                    if ( string.IsNullOrEmpty( Series.Messages[ 0 ].NoteUrl ) )
                    {
                        cell.ToggleTakeNotesButton( false );
                    }
                    else
                    {
                        cell.ToggleTakeNotesButton( true );
                    }

                    // disable the button if there's no watch URL to share
                    if ( string.IsNullOrEmpty( Series.Messages[row].WatchUrl ) )
                    {
                        cell.ToggleShareButton( false );
                    }
                    else
                    {
                        cell.ToggleShareButton( true );
                    }

                    //PendingCellHeight = rowHeight;
                }
                else
                {
                    // dummy row for padding.
                    cell.HideControls( true );
                }

                return cell;
            }

            public void RowButtonClicked( int row, int buttonIndex )
            {
                // we dont need to check for the dummy row being clicked, because
                // its buttons are hidden, making it impossible.
                Parent.RowClicked( row, buttonIndex );
            }
        }

        /// <summary>
        /// A wrapper class that consolidates the message, it's thumbnail and podcast status
        /// </summary>
        public class MessageEntry
        {
            public Series.Message Message { get; set; }
        }

        public Series Series { get; set; }
        public UIImage SeriesBillboard { get; set; }
        public List<MessageEntry> Messages { get; set; }
        bool IsVisible { get; set; }
        UITableView SeriesTable { get; set; }

        public NotesDetailsUIViewController ( Task parentTask )
		{
            Task = parentTask;
		}

        public override void ViewDidLoad()
        {
            base.ViewDidLoad( );

            // setup the table view and general background view colors
            SeriesTable = new UITableView( );
            SeriesTable.Layer.AnchorPoint = CGPoint.Empty;
            SeriesTable.BackgroundColor = Rock.Mobile.UI.Util.GetUIColor( ControlStylingConfig.BackgroundColor );
            SeriesTable.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            View.AddSubview( SeriesTable );

            // setup the messages list
            Messages = new List<MessageEntry>();
            TableSource source = new TableSource( this, Messages, Series );
            SeriesTable.Source = source;
            SeriesTable.Delegate = new TableViewDelegate( source, Task.NavToolbar );

            // log the series they tapped on.
            MessageAnalytic.Instance.Trigger( MessageAnalytic.BrowseSeries, Series.Name );


            IsVisible = true;

            for ( int i = 0; i < Series.Messages.Count; i++ )
            {
                MessageEntry messageEntry = new MessageEntry();
                Messages.Add( messageEntry );

                messageEntry.Message = Series.Messages[ i ];
            }


            // do we have the real image?
            if( TryLoadImage( NotesTask.FormatBillboardImageName( Series.Name ) ) == false )
            {
                // no, so use a placeholder and request the actual image
                SeriesBillboard = new UIImage( NSBundle.MainBundle.BundlePath + "/" + PrivateNoteConfig.NotesMainPlaceholder );

                // request!
                FileCache.Instance.DownloadFileToCache( Series.BillboardUrl, NotesTask.FormatBillboardImageName( Series.Name ), null,
                    delegate
                    {
                        Rock.Mobile.Threading.Util.PerformOnUIThread( 
                            delegate
                            {
                                if( IsVisible == true )
                                {
                                    TryLoadImage( NotesTask.FormatBillboardImageName( Series.Name ) );
                                }
                            });
                    } );
            }
        }

        bool TryLoadImage( string imageName )
        {
            bool success = false;

            if( FileCache.Instance.FileExists( imageName ) == true )
            {
                MemoryStream imageStream = null;
                try
                {
                    imageStream = (MemoryStream)FileCache.Instance.LoadFile( imageName );

                    NSData imageData = NSData.FromStream( imageStream );
                    SeriesBillboard = new UIImage( imageData );

                    SeriesTable.ReloadData( );

                    success = true;
                }
                catch( Exception )
                {
                    FileCache.Instance.RemoveFile( imageName );
                    Rock.Mobile.Util.Debug.WriteLine( string.Format( "Image {0} is corrupt. Removing.", imageName ) );
                }
                imageStream.Dispose( );
            }

            return success;
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            IsVisible = false;
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            // adjust the table height for our navbar.
            // We MUST do it here, and we also have to set ContentType to Top, as opposed to ScaleToFill, on the view itself,
            // or our changes will be overwritten
            SeriesTable.Frame = new CGRect( 0, 0, View.Bounds.Width, View.Bounds.Height );
        }

        public override void LayoutChanged( )
        {
            base.LayoutChanged( );

            // if the layout is changed, the simplest way to fix the UI is to recreate the table source
            TableSource source = new TableSource( this, Messages, Series );
            SeriesTable.Source = source;
            SeriesTable.Delegate = new TableViewDelegate( source, Task.NavToolbar );
            SeriesTable.ReloadData( );
        }

        public void RowClicked( int row, int buttonIndex )
        {
           // 0 would be the podcast button
            if ( buttonIndex == 0 )
            {
                SelectPodcastFormat( row );
            }
            // 1 would be the Notes button
            else if ( buttonIndex == 1 )
            {
                // maybe technically a hack...we know our parent is a NoteTask,
                // so cast it so we can use the existing NotesViewController.
                NotesTask noteTask = Task as NotesTask;
                if ( noteTask != null )
                {
                    noteTask.NoteController.NoteName = Series.Messages[row].Name;
                    noteTask.NoteController.NoteUrl = Series.Messages[row].NoteUrl;
                    noteTask.NoteController.StyleSheetDefaultHostDomain = RockLaunchData.Instance.Data.NoteDB.HostDomain;

                    Task.PerformSegue( this, noteTask.NoteController );
                }
            }
            // 2 would be the share button
            else if ( buttonIndex == 2 )
            {
                string noteString = MessagesStrings.Watch_Share_Header_Html + string.Format( MessagesStrings.Watch_Share_Body_Html, Series.Messages[row].ShareUrl, Series.Messages[row].Name );

                var items = new NSObject[] { new NSString( noteString ) };

                UIActivityViewController shareController = new UIActivityViewController( items, null );
                shareController.SetValueForKey( new NSString( MessagesStrings.Watch_Share_Subject ), new NSString( "subject" ) );

                shareController.ExcludedActivityTypes = new NSString[] { UIActivityType.PostToFacebook,
                UIActivityType.AirDrop,
                UIActivityType.PostToTwitter,
                UIActivityType.CopyToPasteboard,
                UIActivityType.Message };

                // if devices like an iPad want an anchor, set it
                if ( shareController.PopoverPresentationController != null )
                {
                    shareController.PopoverPresentationController.SourceView = Task.NavToolbar;
                }
                PresentViewController( shareController, true, null );
            }
        }

        void SelectPodcastFormat( int row )
        {

            UIAlertController actionsheet = UIAlertController.Create( MessagesStrings.Podcast_Action_Title, 
                                                                      MessagesStrings.Podcast_Action_Subtitle, 
                                                                      UIAlertControllerStyle.ActionSheet );

            // setup Audio Only
            if ( !string.IsNullOrEmpty( Series.Messages[row].AudioUrl ) )
            {
                UIAlertAction audioOption = UIAlertAction.Create( MessagesStrings.Podcast_Action_Audio, UIAlertActionStyle.Default, delegate ( UIAlertAction obj )
                {
                    NotesWatchUIViewController viewController = new NotesWatchUIViewController();
                    viewController.MediaUrl = Series.Messages[row].AudioUrl;
                    viewController.ShareUrl = Series.Messages[row].ShareUrl;
                    viewController.Name = Series.Messages[row].Name;
                    viewController.AudioOnly = true;

                    Task.PerformSegue( this, viewController );
                } );

                actionsheet.AddAction( audioOption );
            }

            // setup Video
            if ( !string.IsNullOrEmpty( Series.Messages[row].WatchUrl ) )
            {
                UIAlertAction videoOption = UIAlertAction.Create( MessagesStrings.Podcast_Action_Video, UIAlertActionStyle.Default, delegate ( UIAlertAction obj )
                {
                    NotesWatchUIViewController viewController = new NotesWatchUIViewController();
                    viewController.MediaUrl = Series.Messages[row].WatchUrl;
                    viewController.ShareUrl = Series.Messages[row].ShareUrl;
                    viewController.Name = Series.Messages[row].Name;
                    viewController.AudioOnly = false;

                    Task.PerformSegue( this, viewController );
                } );

                actionsheet.AddAction( videoOption );
            }

            // setup Cancel
            UIAlertAction cancelAction = UIAlertAction.Create( GeneralStrings.Cancel, UIAlertActionStyle.Cancel, delegate { } );

            actionsheet.AddAction( cancelAction );
            PresentViewController( actionsheet, true, null );
        }
	}
}
