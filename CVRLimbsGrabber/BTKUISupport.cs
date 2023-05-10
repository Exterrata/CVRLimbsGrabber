using BTKUILib;
using BTKUILib.UIObjects;
using MelonLoader;
using System;

namespace Koneko;
internal class BTKUISupport
{
    public static void Initialize()
    {
        var misc = QuickMenuAPI.MiscTabPage;
        var mainCatagory = misc.AddCategory("CVR Limbs Grabber");
        var limbPage = mainCatagory.AddPage("Enabled Limbs", "", "Enable And Disable Limbs", "CVRLimbsGrabber");
        var limbCatagory = limbPage.AddCategory("Enable And Disable Limbs");
        var settingPage = mainCatagory.AddPage("Settings", "", "LimbGrabber Settings", "CVRLimbsGrabber");
        var settingCatagory = settingPage.AddCategory("LimbGrabber Settings");

        AddToggle(ref limbCatagory, LimbGrabber.EnableHands);
        AddToggle(ref limbCatagory, LimbGrabber.EnableFeet);
        AddToggle(ref limbCatagory, LimbGrabber.EnableHead);
        AddToggle(ref limbCatagory, LimbGrabber.EnableHip);
        AddToggle(ref limbCatagory, LimbGrabber.EnableRoot);

        AddToggle(ref settingCatagory, LimbGrabber.Friend);
        AddToggle(ref settingCatagory, LimbGrabber.EnablePose);
        AddToggle(ref settingCatagory, LimbGrabber.PreserveMomentum);
        AddToggle(ref settingCatagory, LimbGrabber.RagdollRelease);
        AddSlider(ref settingPage, LimbGrabber.VelocityMultiplier, 0.1f, 100, 1);
        AddSlider(ref settingPage, LimbGrabber.GravityMultiplier, 0, 100, 1);
        AddSlider(ref settingPage, LimbGrabber.Distance, 0.01f, 1);

        AddToggle(ref mainCatagory, LimbGrabber.Enabled);
        mainCatagory.AddButton("Release All", "", "Release All").OnPress += new Action(() => LimbGrabber.ReleaseAll());
    }

    private static void AddToggle(ref Category category, MelonPreferences_Entry<bool> entry)
    {
        category.AddToggle(entry.DisplayName, entry.Description, entry.Value).OnValueUpdated += b => entry.Value = b;
    }

    private static void AddSlider(ref Page page, MelonPreferences_Entry<float> entry, float min, float max, int decimalPlaces = 2)
    {
        page.AddSlider(entry.DisplayName, entry.Description, entry.Value, min, max, decimalPlaces).OnValueUpdated += f => entry.Value = f;
    }
}