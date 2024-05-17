namespace Dec.DiscordIPC.Commandz
{
    public class GetChannel
    {
        public class Args
        {
            public string channel_id { get; set; }
        }

        public class Data
        {
            public Channel channel { get; set; }
        }

        public class Channel
        {
            public List<Member> members { get; set; }
        }

        public class Member
        {
            public User user { get; set; }
        }

        public class User
        {
            public string username { get; set; }
            public string id { get; set; }
        }
    }

    public class SetUserVoiceSettings
    {
        public class Args
        {
            public string user_id { get; set; }
            public int volume { get; set; }
        }
    }
}
