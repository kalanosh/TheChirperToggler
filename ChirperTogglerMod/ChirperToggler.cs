using System;
using System.Collections.Generic;
using System.IO;

using ICities;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using System.Xml.Serialization;

/// <summary>
/// Automatically removes outdated chirper messages.
/// </summary>
namespace ChirperTogglerMod
{

    /// <summary>
    /// Metadata of the modification.
    /// </summary>
    public class ChirperToggler : IUserMod
    {



        /// <summary>
        /// Gets the name of the modification.
        /// </summary> 
        public string Name
        {
            get
            {
                return "ChirperToggler";
            }
        }

        /// <summary>
        /// Gets the description of the modification.
        /// </summary>
        public string Description
        {
            get
            {
                return "Add button top right of screen to toggle the chirper on and off. Messages missed are still availabe when Chirper is toggled back on.";
            }
        }




    }

    public class LoadingExtension : LoadingExtensionBase
    {
		//Some static variables, colors should be constants
        static Color32 buttonOnColor = new Color32(51, 153, 255, 150);
        static Color32 buttonOffColor = new Color32(194, 209, 224, 150);
        static UIButton ButtonToggle;
        static UIButton ButtonPosition;
        bool MouseDragging = false;
        static ChirperControl chirpeControl = new ChirperControl();

		
		//Setup mod on level load
        public override void OnLevelLoaded(LoadMode mode)
        {
            addTogglButton();
            addPositionChangeButton();

        }

		
		//Destroy my buttons!
		//I notice if I move the button and then reload 
		//there were two buttons
		//so safe to say, call Unload and dispose of stuff.
        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            Serialize();
            UIComponent.Destroy(ButtonPosition);
            UIComponent.Destroy(ButtonToggle);
        }

        private void addTogglButton()
        {

            // Get the UIView object. This seems to be the top-level object for most
            // of the UI.
            var uiView = UIView.GetAView();

            // Add a new button to the view.
            var button = (UIButton)uiView.AddUIComponent(typeof(UIButton));
            ButtonToggle = button;

            // Set the text to show on the button.
            button.text = "Chirper On";

            // Set the button dimensions.
            button.width = 70;
            button.height = 25;
            //button.autoSize = true;

            // Style the button to look like a menu button.
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenu";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.textScale = .6F;
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.focusedTextColor = button.textColor;
            button.color = buttonOnColor;
            button.focusedColor = button.color;


            // Enable button sounds.
            button.playAudioEvents = true;

            // Begin loading data for mod
            List<string> configData = Deserialize();
            
	
            if (configData == null)
            {	
				//If not file exist then default setup
                button.absolutePosition = new Vector3(60, 10);
                ChirperSetup(true, ButtonToggle);
            }
            else
            {
				//if it exist then setup
				//split a string that represents a vector3
                string[] lastPos = configData[1].Split(',');

				//parse each string in array into floats, add to Vector3 x,y,z
                Vector3 ButtonPosition = new Vector3(float.Parse(lastPos[0]), float.Parse(lastPos[1]), float.Parse(lastPos[2]));
                button.absolutePosition = ButtonPosition;
				
				//Legacy i didnt have a variable for on/off state
				//this trys to parse the string as bool, if not they are just switching versions
				//and need to set to default, file save will convert their save correctly after
                bool flag;
                var value = bool.TryParse(configData[0], out flag);
				//if no bool value exist set Chirper to ON
                if (flag == false)
                    value = true;
				
                ChirperSetup(value, ButtonToggle);
            }

            // Respond to button click.
            button.eventClick += ChirperButtonClick;
        }



        private void addPositionChangeButton()
        {
            // Get the UIView object. This seems to be the top-level object for most
            // of the UI.
            var uiView = UIView.GetAView();



            // Add a new button to the view.
            var button = (UIButton)uiView.AddUIComponent(typeof(UIButton));
            ButtonPosition = button;

            // Set the text to show on the button.
            button.text = "Position";

            // Set the button dimensions.
            button.width = 70;
            button.height = 14;

            // Style the button to look like a menu button.
            button.normalBgSprite = "ButtonMenu";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.focusedBgSprite = "ButtonMenuFocused";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.textColor = new Color32(255, 255, 255, 255);
            button.textScale = .5F;
            button.disabledTextColor = new Color32(7, 7, 7, 255);
            button.hoveredTextColor = new Color32(7, 132, 255, 255);
            //button.focusedTextColor = new Color32(255, 255, 255, 255);
            button.pressedTextColor = new Color32(30, 30, 44, 255);
            button.color = buttonOnColor;

            
			//Create tooltip UIComponent using UILabel
            var tooltipBox = (UILabel)uiView.AddUIComponent(typeof(UILabel));
            tooltipBox.color = new Color32(50, 50, 50, 200);
            tooltipBox.isVisible = false;
            tooltipBox.autoSize = true;
            tooltipBox.textScale = .8f;
            tooltipBox.isInteractive = false;




  

            // Enable button sounds.
            button.playAudioEvents = true;

            // Place the button.
            //button.transformPosition = new Vector3(-1.65f, 0.97f);
            button.absolutePosition = new Vector3(ButtonToggle.absolutePosition.x, ButtonToggle.absolutePosition.y + 25);

            //button tooltip setup
            button.tooltip = "Click and drag to reposition";
            button.tooltipBox = tooltipBox;
            button.tooltipAnchor = UITooltipAnchor.Floating;
            button.RefreshTooltip();
            button.eventTooltipShow += MoveToolTip;

            // Respond to button click.
            button.eventMouseUp += EndDrag;
            button.eventDragStart += DragChangePos;




        }
		
		//Tooltip would let me reposition it easily, had to had an event.
        private void MoveToolTip(UIComponent component, UITooltipEventParameter eventParam)
        {
            component.tooltipBox.position = new Vector3(component.position.x, component.position.y - 15, 0);
            component.Update();
        }
		
		//If mousedragging is on from DragChangePos then UIComponent has been dropped
		//You can do something here as if eventDragDrop was called
		//Though i cant get eventDragDrop to work
        private void EndDrag(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (MouseDragging)
            {
                //Unused
            }

        }

		//Used for eventDragStart, it allow me to drag a UIComponent like drag and drop
        private void DragChangePos(UIComponent component, UIDragEventParameter eventParam)
        {
            ButtonPosition.absolutePosition = new Vector3(Input.mousePosition.x - 45, (1080 - Input.mousePosition.y) - 7, 0);
            ButtonToggle.absolutePosition = new Vector3(ButtonPosition.absolutePosition.x, ButtonPosition.absolutePosition.y - 25);
            component.tooltipBox.isEnabled = false;
            component.tooltipBox.isVisible = false;

            MouseDragging = true;

            eventParam.target.Update();


        }
		
		
		//Save my mods state in XML file
        static public void Serialize()
        {
            
            List<string> config = new List<string>();
            var filepath = @"Files\Mods\ChirperToggler\";
            
            config.Add(chirpeControl.ToggleState.ToString());
            config.Add(ButtonToggle.absolutePosition.ToString().Trim('(' , ')' , ' ' ));
						
			//check if directory exist
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            XmlSerializer serialize = new XmlSerializer(typeof(List<string>));
            using (TextWriter writer = new StreamWriter(filepath+"settings.xml"))
            {

                serialize.Serialize(writer, config);

            }

            Debug.Print(@"Saved to \SteamApps\common\Cities_Skylines\Files\Mods\ChirperToggler\settings.xml");
            //DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, @"Saved to \SteamApps\common\Cities_Skylines\Files\Mods\ChirperToggler\settings.xml");
        }
		
		//Load my mods state from my xml file
        static public List<string> Deserialize()
        {
            var filepath = @"Files\Mods\ChirperToggler\";
			
			//check if directory exist
            if (!Directory.Exists(filepath))
                Directory.CreateDirectory(filepath);

            XmlSerializer deserialize = new XmlSerializer(typeof(List<string>));

            try
            {
                using (TextReader reader = new StreamReader(filepath + "settings.xml"))
                {
                    List<string> config = (List<string>)deserialize.Deserialize(reader);
                    return config;

                }
            }
            catch (Exception e)
            {
					
                Debug.Print(e.Message); //Catch the save error, just in case.
            }

            return null;

        }

		
        private void ChirperButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
			//Toggle button. If toggle return True its on, off its off
            var button = component as UIButton;
            if (chirpeControl.Toggle())
            {
				ButtonOn(button);
            }
            else
            {
                ButtonOff(button);
            }
        }

        private void ChirperSetup(bool onOff, UIButton button)
        {

			//Setup button based on file, overload method, same return
            if (chirpeControl.Toggle(onOff))
            {
				ButtonOn(button);
            }
            else
            {
                ButtonOff(button);
            }


        }
		
		private void ButtonOn(UIButton button)
		{
			button.text = "Chirper On";
			button.normalBgSprite = "ButtonMenu";
			button.hoveredBgSprite = "ButtonMenuHovered";
			button.focusedBgSprite = "ButtonMenu";
			button.color = buttonOnColor;
			button.focusedColor = buttonOnColor;
		}
		
		private void ButtonOff(UIButton button)
		{
			button.text = "Chirper Off";
			button.normalBgSprite = "ButtonMenuDisabled";
			button.hoveredBgSprite = "ButtonMenuDisabled";
			button.focusedBgSprite = "ButtonMenuDisabled";
			button.color = buttonOffColor;
			button.focusedColor = buttonOffColor;
		}



    }

	//Just a quick extension to catch someone saving a game to save the mods position and state
    public class SavingGame : SerializableDataExtensionBase
    {
        public override void OnSaveData()
        {
            base.OnSaveData();
            LoadingExtension.Serialize();
            
        }
    }
	
	
	//Easier access to the Games F7 Debug window
    static class Debug 
    { 
        public static void Print(string message)
        {
             DebugOutputPanel.AddMessage(ColossalFramework.Plugins.PluginManager.MessageType.Message, @message);
        }
        
    }

    public class ChirperControl : ChirperExtensionBase
    {
        private static IChirper thisChirper;
        private static bool toggleState = true;
        public bool ToggleState { get { return toggleState; } }

        public override void OnCreated(IChirper chirper)
        {
            if (thisChirper == null)
                thisChirper = chirper;
        }

        public bool Toggle()
        {
            if (toggleState == true)
            {
                //Toggle Chirper Off
                thisChirper.ShowBuiltinChirper(false);
                toggleState = false;
                return false;
            }
            //thisChirper.DestroyBuiltinChirper(); 
            else
            {
                //Toggle Chirper On
                thisChirper.ShowBuiltinChirper(true);
                toggleState = true;
                return true;
                
            }
            
        }
       

        public bool Toggle(bool onOff)
        {

            if (onOff == true)
            {
                //Toggle Chirper Off
                thisChirper.ShowBuiltinChirper(true);
                toggleState = true;
                return true;
            }
            //thisChirper.DestroyBuiltinChirper(); 
            else
            {
                //Toggle Chirper On
                thisChirper.ShowBuiltinChirper(false);
                toggleState = false;
                return false;
            }
        }

        public override void OnUpdate()
        {

        }
    }

}


