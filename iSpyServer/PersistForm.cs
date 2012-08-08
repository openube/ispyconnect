using System;
using System.Data;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

public class PersistWindowState : System.ComponentModel.Component
{
    // event info that allows form to persist extra window state data
    public delegate void WindowStateDelegate(object sender, RegistryKey key);
    public event WindowStateDelegate LoadStateEvent;
    public event WindowStateDelegate SaveStateEvent;

    private Form m_parent;
    private string m_regPath;
    private int m_normalLeft;
    private int m_normalTop;
    private int m_normalWidth;
    private int m_normalHeight;
    private FormWindowState m_windowState;
    private bool m_allowSaveMinimized = false;

    public PersistWindowState()
    {
    }

    public Form Parent
    {
        set
        {
            m_parent = value;

            // subscribe to parent form's events
            m_parent.Closing += new System.ComponentModel.CancelEventHandler(OnClosing);
            m_parent.Resize += new System.EventHandler(OnResize);
            m_parent.Move += new System.EventHandler(OnMove);
            m_parent.Load += new System.EventHandler(OnLoad);

            // get initial width and height in case form is never resized
            m_normalWidth = m_parent.Width;
            m_normalHeight = m_parent.Height;
        }
        get
        {
            return m_parent;
        }
    }

    // registry key should be set in parent form's constructor
    public string RegistryPath
    {
        set
        {
            m_regPath = value;
        }
        get
        {
            return m_regPath;
        }
    }

    public bool AllowSaveMinimized
    {
        set
        {
            m_allowSaveMinimized = value;
        }
    }

    private void OnResize(object sender, System.EventArgs e)
    {
        // save width and height
        if (m_parent.WindowState == FormWindowState.Normal)
        {
            m_normalWidth = m_parent.Width;
            m_normalHeight = m_parent.Height;
        }
    }

    private void OnMove(object sender, System.EventArgs e)
    {
        // save position
        if (m_parent.WindowState == FormWindowState.Normal)
        {
            m_normalLeft = m_parent.Left;
            m_normalTop = m_parent.Top;
        }
        // save state
        m_windowState = m_parent.WindowState;
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // save position, size and state
        RegistryKey key = Registry.CurrentUser.CreateSubKey(m_regPath);
        key.SetValue("Left", m_normalLeft);
        key.SetValue("Top", m_normalTop);
        key.SetValue("Width", m_normalWidth);
        key.SetValue("Height", m_normalHeight);

        // check if we are allowed to save the state as minimized (not normally)
        if (!m_allowSaveMinimized)
        {
            if (m_windowState == FormWindowState.Minimized)
                m_windowState = FormWindowState.Normal;
        }

        key.SetValue("WindowState", (int)m_windowState);

        // fire SaveState event
        if (SaveStateEvent != null)
            SaveStateEvent(this, key);
    }

    private void OnLoad(object sender, System.EventArgs e)
    {
        // attempt to read state from registry
        RegistryKey key = Registry.CurrentUser.OpenSubKey(m_regPath);
        if (key != null)
        {
            int left = (int)key.GetValue("Left", m_parent.Left);
            int top = (int)key.GetValue("Top", m_parent.Top);
            int width = (int)key.GetValue("Width", m_parent.Width);
            int height = (int)key.GetValue("Height", m_parent.Height);
            FormWindowState windowState = (FormWindowState)key.GetValue("WindowState",
            (int)m_parent.WindowState);

            m_parent.WindowState = windowState;
            m_parent.Location = new Point(left, top);
            m_parent.Size = new Size(width, height);

            // fire LoadState event
            if (LoadStateEvent != null)
                LoadStateEvent(this, key);
        }
    }
}
