﻿using System;
using Driver.Shared.Dto.Users;

namespace Driver.Shared.Dto
{
    public class FolderUnitDto
    {
        public int Id { get; set; }
        public bool IsDeleted { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public AuthorDto Author { get; set; }
        public int SpaceId { get; set; }
        public int ParentId { get; set; }
        public bool CanRead { get; set; }
        public bool CanModify { get; set; }
    }
}
