﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Principal;
using System.Threading.Tasks;
using Driver.Shared.Dto;
using Drive.DataAccess.Entities;
using Drive.DataAccess.Interfaces;
using Driver.Shared.Dto.Users;

namespace Drive.WebHost.Services
{
    public class FileService : IFileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;

        public FileService(IUnitOfWork unitOfWork, IUsersService usersService)
        {
            _unitOfWork = unitOfWork;
            _usersService = usersService;
        }

        public async Task<IEnumerable<FileUnitDto>> GetAllAsync()
        {
            var data = await _unitOfWork.Files.GetAllAsync();

            if (data != null)
            {
                var dto = from d in data
                    select new FileUnitDto()
                    {
                        Id = d.Id,
                        IsDeleted = d.IsDeleted,
                        FileType = d.FileType.ToString(),
                        Name = d.Name,
                        Description = d.Description,
                        SpaceId = d.Space.Id
                    };

                return dto;
            }
            return null;
        }

        public async Task<FileUnitDto> GetAsync(int id)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            if (file != null)
            {
                return new FileUnitDto
                {
                    Id = file.Id,
                    IsDeleted = file.IsDeleted,
                    FileType = file.FileType.ToString(),
                    Name = file.Name,
                    Description = file.Description,
                    SpaceId = file.Space.Id
                };
            }
            return null;
        }

        public async Task<FileUnitDto> CreateAsync(FileUnitDto dto)
        {
            var user = await _usersService.GetCurrentUser();
            var space = await _unitOfWork.Spaces.GetByIdAsync(dto.SpaceId);
            var parentFolder = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);

            if (space != null)
            {
                var file = new FileUnit()
                {
                    Name = dto.Name,
                    //Link = dto.Link,
                    FileType = FileType.None,//(FileType)Enum.Parse(typeof(FileType), dto.FileType),
                    Description = dto.Description,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    IsDeleted = false,
                    Space = space,
                    Parent = parentFolder,
                    Owner = await _unitOfWork.Users.Query.FirstOrDefaultAsync(u => u.GlobalId == user.serverUserId)
                };


                _unitOfWork.Files.Create(file);
                await _unitOfWork.SaveChangesAsync();

                dto.Id = file.Id;
                dto.CreatedAt = file.CreatedAt;
                dto.LastModified = file.LastModified;
                dto.Author = new AuthorDto() { Id = file.Owner.Id, Name = user.name +' '+ user.surname };

                return dto;
            }
            return null;
        }

        public async Task<FileUnitDto> UpdateAsync(int id, FileUnitDto dto)
        {
            var file = await _unitOfWork.Files.GetByIdAsync(id);

            file.Name = dto.Name;
            file.FileType = FileType.None;
            file.Description = dto.Description;
            file.IsDeleted = dto.IsDeleted;
            file.LastModified = DateTime.Now;
            file.Link = dto.Link;

            await _unitOfWork.SaveChangesAsync();

            return dto;
        }

        public async Task DeleteAsync(int id)
        {
            _unitOfWork.Files.Delete(id);
            await _unitOfWork.SaveChangesAsync();
        }

        public void Dispose()
        {
            _unitOfWork.Dispose();
        }
    }
}