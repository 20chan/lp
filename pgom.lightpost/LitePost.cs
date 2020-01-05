using System;

namespace pgom.lightpost {
    public class LitePost {
        public int Id { get; set; }
        public DateTime WrittenDate { get; set; }
        public bool Public { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
