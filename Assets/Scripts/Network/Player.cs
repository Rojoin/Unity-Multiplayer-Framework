using System;

[Serializable]
public struct Player
{
    public int id;
    public string nameTag;

    public Player(int id, string nameTag)
    {
        this.id = id;
        this.nameTag = nameTag;
    }
}