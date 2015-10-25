﻿/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using FSO.Files;
using FSO.Client.Network;
using FSO.Common.Utils;
using FSO.Server.Protocol.CitySelector;
using FSO.Vitaboy;
using FSO.Client.Regulators;
using Ninject;
using FSO.Client.Controllers;

namespace FSO.Client.UI.Screens
{
    public class PersonSelection : GameScreen
    {
        /// <summary>
        /// Values from the UIScript
        /// </summary>
        public Texture2D BackgroundImage { get; set; }
        public Texture2D BackgroundImageDialog { get; set; }

        public Texture2D SimCreateButtonImage { get; set; }
        public Texture2D SimSelectButtonImage { get; set; }
        public Texture2D HouseButtonTemplateImage { get; set; }
        public Texture2D CityButtonTemplateImage { get; set; }
        public Texture2D CityHouseButtonAlpha { get; set; }

        public UIButton CreditsButton { get; set; }
        private List<PersonSlot> m_PersonSlots { get; set; }
        private UIButton m_ExitButton;

        public LoginRegulator LoginRegulator;

        public PersonSelection(LoginRegulator loginRegulator)
        {
            //Arrange UI
            this.LoginRegulator = loginRegulator;
            
            UIScript ui = null;
            if (GlobalSettings.Default.ScaleUI)
            {
                ui = this.RenderScript("personselection.uis");
                this.Scale800x600 = true;
            }
            else
            {
                ui = this.RenderScript("personselection" + (ScreenWidth == 1024 ? "1024" : "") + ".uis");
            }

            m_ExitButton = (UIButton)ui["ExitButton"];

            var numSlots = 3;
            m_PersonSlots = new List<PersonSlot>();

            for (var i = 0; i < numSlots; i++)
            {
                var index = (i + 1).ToString();

                /** Tab Background **/
                var tabBackground = ui.Create<UIImage>("TabBackgroundImage" + index);
                this.Add(tabBackground);

                var enterTabImage = ui.Create<UIImage>("EnterTabImage" + index);
                this.Add(enterTabImage);

                var descTabImage = ui.Create<UIImage>("DescriptionTabImage" + index);
                this.Add(descTabImage);

                var descTabBgImage = ui.Create<UIImage>("DescriptionTabBackgroundImage" + index);
                var enterIcons = ui.Create<UIImage>("EnterTabBackgroundImage" + index);

                var personSlot = new PersonSlot(this)
                {
                    AvatarButton = (UIButton)ui["AvatarButton" + index],
                    CityButton = (UIButton)ui["CityButton" + index],
                    HouseButton = (UIButton)ui["HouseButton" + index],
                    EnterTabButton = (UIButton)ui["EnterTabButton" + index],
                    DescTabButton = (UIButton)ui["DescriptionTabButton" + index],
                    NewAvatarButton = (UIButton)ui["NewAvatarButton" + index],
                    DeleteAvatarButton = (UIButton)ui["DeleteAvatarButton" + index],
                    PersonNameText = (UILabel)ui["PersonNameText" + index],
                    PersonDescriptionScrollUpButton = (UIButton)ui["PersonDescriptionScrollUpButton" + index],
                    PersonDescriptionScrollDownButton = (UIButton)ui["PersonDescriptionScrollDownButton" + index],
                    PersonDescriptionSlider = (UISlider)ui["PersonDescriptionSlider" + index],
                    CityNameText = (UILabel)ui["CityNameText" + index],
                    HouseNameText = (UILabel)ui["HouseNameText" + index],
                    PersonDescriptionText = (UITextEdit)ui["PersonDescriptionText" + index],
                    DescriptionTabBackgroundImage = descTabBgImage,
                    EnterTabBackgroundImage = enterIcons,

                    TabBackground = tabBackground,
                    TabEnterBackground = enterTabImage,
                    TabDescBackground = descTabImage
                };

                this.AddBefore(descTabBgImage, personSlot.PersonDescriptionText);
                this.AddBefore(enterIcons, personSlot.CityButton);

                personSlot.Init();
                personSlot.SetSlotAvailable(true);
                m_PersonSlots.Add(personSlot);
                
                if(i < loginRegulator.Avatars.Count)
                {
                    var avatar = loginRegulator.Avatars[i];
                    personSlot.DisplayAvatar(avatar);
                }
            }

            this.AddAt(0, new UIImage(BackgroundImage));
            if (BackgroundImageDialog != null)
            {
                this.AddAt(1, new UIImage(BackgroundImageDialog)
                {
                    X = 112,
                    Y = 84
                });
            }

            /**
             * Button plumbing
             */
            CreditsButton.OnButtonClick += new ButtonClickDelegate(CreditsButton_OnButtonClick);
            m_ExitButton.OnButtonClick += new ButtonClickDelegate(m_ExitButton_OnButtonClick);

            /**
             * Music
             */
            var tracks = new string[]{
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas1_v2.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas2_v2.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas3.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas4.mp3",
                GlobalSettings.Default.StartupPath + "\\music\\modes\\select\\tsosas5.mp3"
            };
            PlayBackgroundMusic(tracks);
        }

        /// <summary>
        /// Device was reset, SceneManager called Content.Unload(), so reload everything.
        /// </summary>
        /// <param name="Device">The device.</param>
        public override void DeviceReset(GraphicsDevice Device)
        {
            foreach(var slot in m_PersonSlots)
            {
                slot.DeviceReset(Device);
            }
            CalculateMatrix();
        }
        
        
        
        
        private void m_ExitButton_OnButtonClick(UIElement button)
        {
            UIScreen.ShowDialog(new UIExitDialog(), true);
        }

        private void CreditsButton_OnButtonClick(UIElement button)
        {
            /** Show the credits screen **/
            GameFacade.Controller.ShowCredits();
        }

        public void ShowCitySelector(List<ShardStatusItem> shards, Callback<ShardStatusItem> onOk)
        {
            var cityPicker = new UICitySelector(shards);
            cityPicker.OkButton.OnButtonClick += (UIElement btn) =>
            {
                onOk(cityPicker.SelectedShard);
            };
            ShowDialog(cityPicker, true);
        }
    }

    public class PersonSlot
    {
        public UIButton CityButton { get; set; }
        public UIButton AvatarButton { get; set; }
        public UIButton HouseButton { get; set; }
        public UIButton EnterTabButton { get; set; }
        public UIButton DescTabButton { get; set; }
        public UIButton NewAvatarButton { get; set; }
        public UIButton DeleteAvatarButton { get; set; }

        public UIImage TabBackground { get; set; }
        public UIImage TabEnterBackground { get; set; }
        public UIImage TabDescBackground { get; set; }
        public UIImage EnterTabBackgroundImage { get; set; }

        public UILabel PersonNameText { get; set; }
        public UILabel CityNameText { get; set; }
        public UILabel HouseNameText { get; set; }

        public UIButton PersonDescriptionScrollUpButton { get; set; }
        public UIButton PersonDescriptionScrollDownButton { get; set; }
        public UISlider PersonDescriptionSlider { get; set; }
        public UITextEdit PersonDescriptionText { get; set; }
        public UIImage DescriptionTabBackgroundImage { get; set; }

        private PersonSelection Screen { get; set; }
        public AvatarData Avatar;
        private UIImage CityThumb { get; set; }

        private UISim Sim;

        public PersonSlot(PersonSelection screen)
        {
            this.Screen = screen;
        }

        /// <summary>
        /// Setup UI events
        /// </summary>
        public void Init()
        {
            /** Textures **/
            AvatarButton.Texture = Screen.SimCreateButtonImage;
            CityButton.Texture = Screen.CityButtonTemplateImage;
            HouseButton.Texture = Screen.HouseButtonTemplateImage;

            /** Send tab stuff to the bottom **/
            Screen.SendToBack(TabBackground, TabEnterBackground, TabDescBackground);

            /** Events **/
            EnterTabButton.OnButtonClick += new ButtonClickDelegate(EnterTabButton_OnButtonClick);
            DescTabButton.OnButtonClick += new ButtonClickDelegate(DescTabButton_OnButtonClick);

            NewAvatarButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
            DeleteAvatarButton.OnButtonClick += new ButtonClickDelegate(DeleteAvatarButton_OnButtonClick);

            PersonDescriptionSlider.AttachButtons(PersonDescriptionScrollUpButton, PersonDescriptionScrollDownButton, 1);
            PersonDescriptionText.AttachSlider(PersonDescriptionSlider);

            CityThumb = new UIImage
            {
                X = CityButton.X + 6,
                Y = CityButton.Y + 6
            };
            CityThumb.SetSize(78, 58);
            Screen.Add(CityThumb);

            Sim = new UISim();
            Sim.Visible = false;
            Sim.Position = AvatarButton.Position + new Vector2(70, (AvatarButton.Size.Y - 35));
            Sim.Size = AvatarButton.Size;

            Screen.Add(Sim);
            SetTab(PersonSlotTab.EnterTab);

            AvatarButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
            CityButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
            HouseButton.OnButtonClick += new ButtonClickDelegate(OnSelect);
        }

        void OnSelect(UIElement button)
        {
            if (this.Avatar != null)
            {
                ((PersonSelectionController)Screen.Controller).ConnectToAvatar(Avatar);
            }
            else
            {
                ((PersonSelectionController)Screen.Controller).CreateAvatar();
            }
        }

        /// <summary>
        /// User clicked the "Retire avatar" button.
        /// </summary>
        private void DeleteAvatarButton_OnButtonClick(UIElement button)
        {
            if (Avatar == null)
            {
                return;
            }

            UIAlertOptions AlertOptions = new UIAlertOptions();
            AlertOptions.Title = "{{169:9}}";
            AlertOptions.Message = "{{169:10}}";
            AlertOptions.Buttons = new UIAlertButton[] {
                new UIAlertButton(UIAlertButtonType.OK, new ButtonClickDelegate(PersonSlot_OnButtonClick)),
                new UIAlertButton(UIAlertButtonType.Cancel)
            };

            UIScreen.GlobalShowAlert(AlertOptions, true);
        }

        /// <summary>
        /// User confirmed character retirement.
        /// </summary>
        private void PersonSlot_OnButtonClick(UIElement button)
        {
            //UIPacketSenders.SendCharacterRetirement(Avatar);
            //UIScreen.RemoveDialog(RetireCharAlert);
        }

        /// <summary>
        /// Display an avatar
        /// </summary>
        /// <param name="avatar"></param>
        public void DisplayAvatar(AvatarData avatar)
        {
            this.Avatar = avatar;
            var isUsed = avatar != null;

            SetSlotAvailable(!isUsed);

            if(avatar == null){
                return;
            }

            PersonNameText.Caption = avatar.Name;
            //PersonDescriptionText.CurrentText = avatar.Description;
            AvatarButton.Texture = Screen.SimSelectButtonImage;

            var shard = Screen.LoginRegulator.Shards.All.First(x => x.Name == avatar.ShardName);
            CityNameText.Caption = shard.Name;

            var cityThumb = GameFacade.GameFilePath(
                "cities\\city_" + shard.Map + "\\thumbnail.bmp");

            Texture2D cityThumbTex =
                TextureUtils.Resize(
                    GameFacade.GraphicsDevice,
                    TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, cityThumb),
                    78,
                    58);
            TextureUtils.CopyAlpha(ref cityThumbTex, Screen.CityHouseButtonAlpha);
            CityThumb.Texture = cityThumbTex;

            SetTab(PersonSlotTab.EnterTab);
            
            Sim.Avatar.BodyOutfitId = avatar.BodyOutfitID;
            Sim.Avatar.HeadOutfitId = avatar.HeadOutfitID;
            Sim.Avatar.Appearance = (AppearanceType)Enum.Parse(typeof(AppearanceType), avatar.AppearanceType.ToString());
            Sim.Visible = true;

            PersonDescriptionText.CurrentText = avatar.Description;
        }

        public void SetSlotAvailable(bool isAvailable)
        {
            if (isAvailable)
            {
                this.Avatar = null;
            }

            EnterTabButton.Disabled = isAvailable;
            DescTabButton.Disabled = isAvailable;

            NewAvatarButton.Visible = isAvailable;
            DeleteAvatarButton.Visible = !isAvailable;

            if (isAvailable)
            {
                TabEnterBackground.Visible = false;
                TabDescBackground.Visible = false;
                TabBackground.Visible = false;
                CityButton.Visible = false;
                HouseButton.Visible = false;
                PersonDescriptionScrollUpButton.Visible = false;
                PersonDescriptionScrollDownButton.Visible = false;
                HouseNameText.Visible = false;
                CityNameText.Visible = false;
                DescriptionTabBackgroundImage.Visible = false;
                EnterTabBackgroundImage.Visible = false;
                PersonDescriptionSlider.Visible = false;
                PersonDescriptionText.Visible = false;

                /*EnterTabButton.OnButtonClick -= new ButtonClickDelegate(EnterTabButton_OnButtonClick);
                DescTabButton.OnButtonClick -= new ButtonClickDelegate(DescTabButton_OnButtonClick);

                DeleteAvatarButton.OnButtonClick -= new ButtonClickDelegate(DeleteAvatarButton_OnButtonClick);*/
            }
            else
                TabBackground.Visible = true;
        }

        public void SetTab(PersonSlotTab tab)
        {
            var isEnter = tab == PersonSlotTab.EnterTab;
            TabEnterBackground.Visible = isEnter;
            TabDescBackground.Visible = !isEnter;

            EnterTabButton.Selected = isEnter;
            DescTabButton.Selected = !isEnter;

            CityNameText.Visible = isEnter;
            CityButton.Visible = isEnter;
            EnterTabBackgroundImage.Visible = isEnter;
            CityThumb.Visible = isEnter;

            PersonDescriptionScrollUpButton.Visible = !isEnter;
            PersonDescriptionScrollDownButton.Visible = !isEnter;

            PersonDescriptionSlider.Visible = !isEnter;
            DeleteAvatarButton.Visible = !isEnter;
            PersonDescriptionText.Visible = !isEnter;
            DescriptionTabBackgroundImage.Visible = !isEnter;
        }

        private void DescTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.DescriptionTab);
        }

        private void EnterTabButton_OnButtonClick(UIElement button)
        {
            SetTab(PersonSlotTab.EnterTab);
        }

        public void DeviceReset(GraphicsDevice device){
            DisplayAvatar(this.Avatar);
        }
    }

    public enum PersonSlotTab
    {
        EnterTab,
        DescriptionTab
    }
}
