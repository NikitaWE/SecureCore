﻿using System;
using System.Windows.Forms;

namespace PasswordManager
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Database.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}