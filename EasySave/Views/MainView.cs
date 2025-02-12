﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Models;

namespace EasySave.Views
{
    public class MainView
    {
        public void Display(string text, bool lineReturn = true)
        {
            if (lineReturn)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.Write(text);
            }
        }

        public int DisplayLang()
        {
            return AskChoice(LanguageManager.GetText("choose_language"), LanguageManager.GetTextArray("languages").ToArray());
        }

        public int DisplayMenu()
        {
            return AskChoice(LanguageManager.GetText("choose_actions"), LanguageManager.GetTextArray("actions").ToArray());
        }
        public int DisplayFormat()
        {
            return AskChoice(LanguageManager.GetText("choose_format"), new string[] { "JSON", "XML" });
        }

        public string AskQuestion(string question)
        {
            Display(question);
            Display("> ", false);

            string? input = "";
            while (input == null || input == "")
            {
                input = Console.ReadLine();
            }
            Display("");
            return input;
        }

        public int AskChoice(string question, string[] choices)
        {
            if (choices == null || choices.Length < 1)
            {
                return -1;
            }

            Display(question);
            for (int i = 0; i < choices.Length; i++)
            {
                Display($"{i + 1} > {choices[i]}");
            }
            Display("> ", false);

            string? input = "";
            int choice = -1;
            while (input == null || input == "" || choice < 1 || choice > choices.Length)
            {
                input = Console.ReadLine();
                int.TryParse(input, out choice);
                if (input == null || input == "" || choice < 1 || choice > choices.Length)
                {
                    Display("> ", false);
                }
            }
            Display("");
            return choice;
        }
    }
}
