using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes the AdMob app ID into the SDK's own settings asset, which is what
/// its build step injects into the Android manifest. Without that manifest
/// entry the Google Mobile Ads SDK throws on initialisation and the app
/// crashes at launch, so this is required configuration, not a preference.
///
/// Reached by reflection because <c>GoogleMobileAdsSettings</c> is an
/// internal type behind its own assembly definition — there is no public
/// API for this. Going through the SDK's own LoadInstance() rather than
/// hand-writing the .asset YAML means the asset is created exactly the way
/// the SDK expects, including when it does not exist yet.
///
/// Run as a batch-mode step so a clean checkout can be configured
/// reproducibly instead of relying on someone remembering an Inspector field.
/// </summary>
public static class AdMobSetup
{
    private const string SettingsTypeName = "GoogleMobileAds.Editor.GoogleMobileAdsSettings";

    public static void ApplyAppId()
    {
        Type settingsType = FindSettingsType();
        if (settingsType == null)
        {
            Fail($"AdMobSetup: {SettingsTypeName} not found — is the SDK imported?");
            return;
        }

        MethodInfo load = settingsType.GetMethod(
            "LoadInstance",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (load == null)
        {
            Fail("AdMobSetup: GoogleMobileAdsSettings.LoadInstance() not found.");
            return;
        }

        var settings = load.Invoke(null, null) as ScriptableObject;
        if (settings == null)
        {
            Fail("AdMobSetup: LoadInstance() returned nothing.");
            return;
        }

        PropertyInfo appIdProperty = settingsType.GetProperty(
            "GoogleMobileAdsAndroidAppId",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (appIdProperty == null)
        {
            Fail("AdMobSetup: GoogleMobileAdsAndroidAppId property not found.");
            return;
        }

        appIdProperty.SetValue(settings, AdUnitIds.AppId);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"AdMobSetup: Android app ID set to {appIdProperty.GetValue(settings)}");
    }

    private static Type FindSettingsType()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(SettingsTypeName);
            if (type != null)
            {
                return type;
            }
        }

        return null;
    }

    private static void Fail(string message)
    {
        Debug.LogError(message);
        EditorApplication.Exit(1);
    }
}
