using System;

[Serializable]
public class Player
{
    public int id;
    public string nameTag;
    public int lives;

    public Player(int id, string nameTag)
    {
        this.id = id;
        this.nameTag = nameTag;
    }

    public Player()
    {
        lives = 5;
    }

}