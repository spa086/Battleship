using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleshipLibrary;

public class User
{
    public int Id { get; set; }

    //todo persist user name between games
    public string? Name { get; set; }

    public List<Cell> ExcludedLocations { get; set; } = new List<Cell>();

    //todo tdd validate ship shape
    public Ship[]? Fleet { get; set; }

}
