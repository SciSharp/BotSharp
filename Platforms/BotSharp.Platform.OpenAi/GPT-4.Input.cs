using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Runtime.InteropServices;

namespace ChatGPT;

public partial class GPT4
{
    [DllImport("user32.dll")]
    internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    internal static extern bool SetClipboardData(uint uFormat, IntPtr data);

    private void InputText(IWebElement textarea, string text)
    {
        OpenClipboard(IntPtr.Zero);
        var ptr = Marshal.StringToHGlobalUni(text);
        SetClipboardData(13, ptr);
        CloseClipboard();

        //textarea.ScrollTopToBottom();
        var actions = new Actions(_driver);
        actions.MoveToElement(textarea)
            .Click(textarea)
            .KeyDown(Keys.LeftControl).KeyDown("v")
            .KeyUp(Keys.LeftControl).KeyUp("v")
            .Perform();
        //textarea.ScrollTopToBottom();
    }

    private void InputNewLine()
    {
        var actions = new Actions(_driver);
        actions.KeyDown(Keys.Shift).KeyDown(Keys.Enter)
            .KeyUp(Keys.Shift).KeyUp(Keys.Enter)
            .Perform();
    }
}
