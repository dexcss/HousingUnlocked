using System;

namespace HousingUnlocked
{
  public class PluginException : Exception
  {
    public PluginException() { }
    public PluginException(string message) : base($"Housing Unlocked Exception: {message}") { }
  }
}
