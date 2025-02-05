using System.Text;
using EasySave.Controllers;

internal class Programme
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        new Controller(args);
    }
}
