﻿using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.Platform.Data.Model
{
    public class RoleEntity : AuditableEntity
    {
        public RoleEntity()
        {
            RolePermissions = new NullCollection<RolePermissionEntity>();
			RoleAssignments = new NullCollection<RoleAssignmentEntity>();
        }

        [Required]
        [StringLength(128)]
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ObservableCollection<RolePermissionEntity> RolePermissions { get; set; }
		public virtual ObservableCollection<RoleAssignmentEntity> RoleAssignments { get; set; }
    }
}
