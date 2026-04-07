using System.Collections.Generic;
using System.Linq;

namespace Quisly.Components;

public static class AvatarCatalog
{
    public record Avatar(string Id, string Name, string ImageUrl);

    // Local PNG avatars located in wwwroot/assets/avatars/
    public static readonly IReadOnlyList<Avatar> All = new List<Avatar>
    {
        new("cat_lady", "Cat Lady", "/assets/avatars/Cat%20Lady.png"),
        new("cat", "Cat", "/assets/avatars/Cat.png"),
        new("ghost", "Ghost", "/assets/avatars/Ghost.png"),
        new("panda", "Panda", "/assets/avatars/Panda.png"),
        new("patient", "Patient", "/assets/avatars/Patient.png"),
        new("pirate", "Pirate", "/assets/avatars/Pirate.png"),
        new("pumpkin_head", "Pumpkin Head", "/assets/avatars/Pumpkin%20Head.png"),
        new("rabbit", "Rabbit", "/assets/avatars/Rabbit.png"),
        new("turtle", "Turtle", "/assets/avatars/Turtle.png"),
        new("witch", "Witch", "/assets/avatars/Witch.png"),
    };

    public static Avatar Get(string? id)
        => All.FirstOrDefault(a => a.Id == id) ?? All[0];

    public static string Url(string? id)
        => Get(id).ImageUrl;
}

