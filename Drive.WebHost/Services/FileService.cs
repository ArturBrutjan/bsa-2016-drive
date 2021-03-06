﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Driver.Shared.Dto;
using Drive.DataAccess.Entities;
using Drive.DataAccess.Interfaces;
using Driver.Shared.Dto.Users;
using System.IO;
using System.Web;
using Google.Apis.Drive.v3;
using Drive.DataAccess.Entities.Pro;
using Driver.Shared.Dto.Pro;
using System.Drawing;
using Driver.Shared.Dto.Events;
using Drive.Logging;
using System.Text.RegularExpressions;
using Drive.DataAccess.Entities.Event;

namespace Drive.WebHost.Services
{
    public class FileService : IFileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUsersService _usersService;
        private readonly ILogger _logger;

        public FileService(IUnitOfWork unitOfWork, IUsersService usersService, ILogger logger)
        {
            _unitOfWork = unitOfWork;
            _usersService = usersService;
            _logger = logger;
        }

        public async Task<IEnumerable<FileUnitDto>> GetAllAsync()
        {
            var files = await _unitOfWork?.Files?.Query.Select(f => new FileUnitDto()
            {
                Id = f.Id,
                Description = f.Description,
                Name = f.Name,
                IsDeleted = f.IsDeleted,
                CreatedAt = f.CreatedAt,
                LastModified = f.LastModified,
                SpaceId = f.Space.Id,
                FileType = f.FileType,
                Link = f.Link
            }).ToListAsync();

            return files;
        }

        public async Task<IEnumerable<FileUnitDto>> GetAllByParentIdAsync(int spaceId, int? parentId)
        {
            var files = await _unitOfWork.Files.Query.Where(f => f.Space.Id == spaceId)
                                                     .Where(f => f.FolderUnit.Id == parentId)
                                                     .Select(f => new FileUnitDto()
                                                     {
                                                         Id = f.Id,
                                                         Description = f.Description,
                                                         Name = f.Name,
                                                         IsDeleted = f.IsDeleted,
                                                         CreatedAt = f.CreatedAt,
                                                         LastModified = f.LastModified,
                                                         SpaceId = f.Space.Id,
                                                         FileType = f.FileType,
                                                         Link = f.Link
                                                     }).ToListAsync();

            return files;
        }

        public async Task<FileUnitDto> GetAsync(int id)
        {
            var file = await _unitOfWork.Files.Query.Where(f => f.Id == id).Select(f => new FileUnitDto()
            {
                Id = f.Id,
                IsDeleted = f.IsDeleted,
                FileType = f.FileType,
                Name = f.Name,
                Description = f.Description,
                SpaceId = f.Space.Id,
                Link = f.Link,
                CreatedAt = f.CreatedAt
            }).SingleOrDefaultAsync();

            return file;
        }

        public async Task<FileUnitDto> GetDeletedAsync(int id)
        {
            var file = await _unitOfWork.Files.Deleted.Where(f => f.Id == id).Select(f => new FileUnitDto()
            {
                Id = f.Id,
                IsDeleted = f.IsDeleted,
                FileType = f.FileType,
                Name = f.Name,
                Description = f.Description,
                SpaceId = f.Space.Id,
                Link = f.Link,
                CreatedAt = f.CreatedAt
            }).SingleOrDefaultAsync();

            return file;
        }

        public async Task<FileUnitDto> CreateAsync(FileUnitDto dto)
        {
            var user = await _usersService.GetCurrentUser();
            var localUser = await _unitOfWork?.Users?.Query.FirstOrDefaultAsync(x => x.GlobalId == user.id);

            var space = await _unitOfWork?.Spaces?.GetByIdAsync(dto.SpaceId);
            var parentFolder = await _unitOfWork?.Folders.GetByIdAsync(dto.ParentId);

            List<User> ReadPermittedUsers = new List<User>();

            ReadPermittedUsers.Add(localUser);

            List<User> ModifyPermittedUsers = new List<User>();

            ModifyPermittedUsers.Add(localUser);


            if (space != null)
            {
                var file = new FileUnit()
                {
                    Name = dto.Name,
                    FileType = dto.FileType,
                    Link = dto.Link,
                    Description = dto.Description,
                    CreatedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    IsDeleted = false,
                    Space = space,
                    FolderUnit = parentFolder,
                    Owner = await _unitOfWork?.Users?.Query.FirstOrDefaultAsync(u => u.GlobalId == user.id),
                    ReadPermittedUsers = ReadPermittedUsers,
                    ModifyPermittedUsers = ModifyPermittedUsers
                };


                _unitOfWork?.Files?.Create(file);
                await _unitOfWork?.SaveChangesAsync();

                dto.Id = file.Id;
                dto.CreatedAt = file.CreatedAt;
                dto.LastModified = file.LastModified;
                dto.Author = new AuthorDto() { Id = file.Owner.Id, Name = user.name + ' ' + user.surname };
                dto.FileType = file.FileType;

                return dto;
            }
            return null;
        }

        public async Task<FileUnitDto> UpdateAsync(int id, FileUnitDto dto)
        {
            var file = await _unitOfWork?.Files?.GetByIdAsync(id);

            if (file == null)
                return null;

            file.Name = dto.Name;
            file.FileType = dto.FileType;
            file.Description = dto.Description;
            file.IsDeleted = dto.IsDeleted;
            file.LastModified = DateTime.Now;
            file.Link = dto.Link;
            if (dto.ParentId != 0)
            {
                file.FolderUnit = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);
            }
            await _unitOfWork?.SaveChangesAsync();

            return dto;
        }

        public async Task<FileUnitDto> UpdateDeletedAsync(int id, int? oldParentId, FileUnitDto dto)
        {
            var file = await _unitOfWork?.Files?.GetByIdDeletedAsync(id);

            if (file == null)
                return null;

            file.Name = dto.Name;
            file.FileType = dto.FileType;
            file.Description = dto.Description;
            file.IsDeleted = dto.IsDeleted;
            file.LastModified = DateTime.Now;
            file.Link = dto.Link;

            var space = await _unitOfWork.Spaces.GetByIdAsync(dto.SpaceId);

            if (oldParentId != null)
            {
                var oldParentFolder =
                    await
                        _unitOfWork.Folders.Query.Include(f => f.DataUnits)
                            .SingleOrDefaultAsync(f => f.Id == oldParentId);

                var list = new List<DataUnit>();
                foreach (var item in oldParentFolder.DataUnits)
                {
                    if (item.Id != file.Id)
                    {
                        list.Add(item);
                    }
                }

                oldParentFolder.DataUnits = list;
            }

            var parentFolder = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);

            file.Space = space;
            file.FolderUnit = parentFolder ?? null;

            await _unitOfWork?.SaveChangesAsync();

            return dto;
        }

        public async Task CreateCopyAsync(int id, FileUnitDto dto)
        {
            var file = await _unitOfWork?.Files.Query
                                                .Include(f => f.ModifyPermittedUsers)
                                                .Include(f => f.ReadPermittedUsers)
                                                .Include(f => f.MorifyPermittedRoles)
                                                .Include(f => f.ReadPermittedRoles)
                                                .SingleOrDefaultAsync(f => f.Id == id); ;

            if (file == null)
                return;

            var space = await _unitOfWork.Spaces.GetByIdAsync(dto.SpaceId);

            var user = await _usersService?.GetCurrentUser();

            Regex regEx = new Regex(@"(.+?)(\.[^.]*$|$)");

            string name = regEx.Match(file.Name)?.Groups[1].Value;
            string extention = regEx.Match(file.Name)?.Groups[2].Value;

            var copies = await _unitOfWork.Files.Query.Where(f => f.Name.StartsWith(name + "-copy") &&
                                        (f.FolderUnit.Id == dto.ParentId || (dto.ParentId == 0 && f.Space.Id == dto.SpaceId))).ToListAsync();

            if (copies.Count > 0)
            {
                int index = 0;
                int maxIndex = 1;
                foreach (var copyStr in copies.Select(c => c.Name.Substring(name.Length)))
                {
                    if (Int32.TryParse(copyStr, out index))
                        if (index > maxIndex)
                            maxIndex = index;
                }
                name = name + (maxIndex + 1).ToString();
            }
            else
            {
                name = name + "-copy";
            }

            var copy = new FileUnit
            {
                Name = name+extention,
                Description = file.Description,
                FileType = file.FileType,
                IsDeleted = file.IsDeleted,
                LastModified = DateTime.Now,
                CreatedAt = DateTime.Now,
                Link = file.Link,
                Space = space,
                Owner = await _unitOfWork.Users.Query.FirstOrDefaultAsync(u => u.GlobalId == user.id),
                ModifyPermittedUsers = file.ModifyPermittedUsers,
                ReadPermittedUsers = file.ReadPermittedUsers,
                MorifyPermittedRoles = file.MorifyPermittedRoles,
                ReadPermittedRoles = file.ReadPermittedRoles
            };

            if (dto.ParentId != 0)
            {
                var parent = await _unitOfWork.Folders.GetByIdAsync(dto.ParentId);
                copy.FolderUnit = parent;
            }
            if (file.FileType == FileType.AcademyPro)
            {
                var course = await _unitOfWork.AcademyProCourses.Query
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .Include(a => a.Lectures.Select(l => l.CodeSamples))
                    .Include(a => a.Lectures.Select(l => l.ContentList))
                    .Include(a => a.Lectures.Select(l => l.HomeTasks))
                    .Include(a => a.Lectures.Select(l => l.Author))
                    .FirstOrDefaultAsync(a => a.FileUnit.Id == file.Id);

                var coursecopy = new AcademyProCourse
                {
                    StartDate = course.StartDate,
                    IsDeleted = false,
                    Tags = new List<Tag>(),
                    FileUnit = copy,
                    Author = course.Author,
                    Lectures = new List<Lecture>()
                };

                course.Tags?.ToList().ForEach(tag =>
                {
                    coursecopy.Tags.Add(_unitOfWork.Tags.Query.FirstOrDefault(x => x.Name == tag.Name) ?? new Tag { Name = tag.Name, IsDeleted = false });
                });

                course.Lectures?.ToList().ForEach(lecture =>
                {
                coursecopy.Lectures.Add(new Lecture
                {
                    Author = lecture.Author,
                    Name = lecture.Name,
                    Description = lecture.Description,
                    IsDeleted = lecture.IsDeleted,
                    CreatedAt = DateTime.Now,
                    StartDate = lecture.StartDate,
                    ModifiedAt = DateTime.Now,
                    CodeSamples = lecture.CodeSamples.Select(cs => new CodeSample
                    {
                        Code = cs.Code,
                        IsDeleted = cs.IsDeleted,
                        Name = cs.Name
                    }).ToList(),
                    ContentList = lecture.ContentList.Select(cl => new ContentLink
                    {
                        Name = cl.Name,
                        IsDeleted = cl.IsDeleted,
                        Description = cl.Description,
                        LinkType = cl.LinkType,
                        Link = cl.Link
                    }).ToList(),
                    HomeTasks = lecture.HomeTasks.Select(ht => new HomeTask
                    {
                        Description = ht.Description,
                        IsDeleted = ht.IsDeleted,
                        DeadlineDate = ht.DeadlineDate
                    }).ToList()
                });
                });

                _unitOfWork.AcademyProCourses.Create(coursecopy);
            }

            if (file.FileType == FileType.Events)
            {
                var originalEvent = await _unitOfWork.Events.Query
                    .Include(e => e.ContentList)
                    .FirstOrDefaultAsync(e => e.FileUnit.Id == file.Id);

                Event eventCopy = new Event
                {
                    IsDeleted = originalEvent.IsDeleted,
                    EventDate = originalEvent.EventDate,
                    EventType = originalEvent.EventType,
                    FileUnit = copy,
                    ContentList = new List<EventContent>()
                };

                originalEvent.ContentList.ToList().ForEach(c =>
                                    eventCopy.ContentList.Add(new EventContent
                                    {
                                        Name = c.Name,
                                        Description = c.Description,
                                        IsDeleted = c.IsDeleted,
                                        ContentType = c.ContentType,
                                        Order = c.Order,
                                        CreatedAt = DateTime.Now,
                                        LastModified = DateTime.Now,
                                        Content = c.Content
                                    })
                );
                _unitOfWork.Events.Create(eventCopy);
            }

            _unitOfWork.Files.Create(copy);

            await _unitOfWork?.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            _unitOfWork?.Files?.Delete(id);
            await _unitOfWork?.SaveChangesAsync();
        }

        public async Task<AcademyProCourseDto> SearchCourse(int fileId)
        {
            var authors = (await _usersService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });

            var result = await _unitOfWork.AcademyProCourses.Query
                    .Include(a => a.FileUnit)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .SingleOrDefaultAsync(c => c.FileUnit.Id == fileId);
            if (result == null)
                return null;
            var resultCourse = new AcademyProCourseDto
            {
                Id = result.Id,
                StartDate = result.StartDate,
                IsDeleted = result.IsDeleted,
                FileUnit = new FileUnitDto
                {
                    Id = result.FileUnit.Id,
                    Name = result.FileUnit.Name,
                    FileType = result.FileUnit.FileType,
                    Description = result.FileUnit.Description,
                    CreatedAt = result.FileUnit.CreatedAt,
                    LastModified = result.FileUnit.LastModified
                },
                Tags = result.Tags.Select(tag => new TagDto
                {
                    Id = tag.Id,
                    Name = tag.Name
                }),
                Author = new AuthorDto { Id = result.Author.Id, GlobalId = result.Author.GlobalId }
            };

            resultCourse.Author.Name = authors.SingleOrDefault(a => a.Id == resultCourse.Author.GlobalId)?.Name;

            return resultCourse;
        }

        public async Task<EventDto> SearchEvent(int id)
        {
            var authors = (await _usersService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });
            var events = await _unitOfWork.Events.Query.Include(c => c.FileUnit).Include(c => c.ContentList).Where(x => x.FileUnit.Id == id).Select(ev => new EventDto
            {
                Id = ev.Id,
                FileUnit = new FileUnitDto
                {
                    Id = ev.FileUnit.Id,
                    Name = ev.FileUnit.Name,
                    Description = ev.FileUnit.Description,
                    Author = new AuthorDto { Id = ev.FileUnit.Owner.Id, GlobalId = ev.FileUnit.Owner.GlobalId }
                },
                EventType = ev.EventType,
                ContentList = ev.ContentList.Select(c => new EventContentDto
                {
                    Id = c.Id,
                    ContentType = c.ContentType,
                    Name = c.Name,
                    Description = c.Description,
                    Content = c.Content,
                    Order = c.Order,
                    CreatedAt = c.CreatedAt,
                    LastModified = c.LastModified,
                }),
                EventDate = ev.EventDate,
                Author = new AuthorDto { Id = ev.FileUnit.Owner.Id, GlobalId = ev.FileUnit.Owner.GlobalId }
            }).FirstOrDefaultAsync();

            events.Author.Name = authors.SingleOrDefault(a => a.Id == events.Author.GlobalId)?.Name;

            return events;
        }

        public async Task<ICollection<AppDto>> FilterApp(FileType fileType)
        {
            string userId = _usersService.CurrentUserId;
            var result = await (from f in _unitOfWork.Files.Query
                                 let userCanRead = f.Space.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                 let roleCanRead = f.Space.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                 let userCanModify = f.Space.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                 let roleCanModify = f.Space.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                 where f.FileType == fileType
                                      && (f.Space.Type == SpaceType.BinarySpace
                                      || f.Space.Owner.GlobalId == userId
                                      || userCanRead || roleCanRead
                                      || userCanModify || roleCanModify)
                                 group new { File = f, userCanRead, roleCanRead, userCanModify, roleCanModify } by f.Space into s
                                 select new AppDto()
                                 {
                                     SpaceId = s.Key.Id,
                                     SpaceType = s.Key.Type,
                                     Name = s.Key.Name,
                                     Files = s.Select(f => new FileUnitDto
                                     {
                                         Description = f.File.Description,
                                         FileType = f.File.FileType,
                                         Id = f.File.Id,
                                         IsDeleted = f.File.IsDeleted,
                                         Name = f.File.Name,
                                         CreatedAt = f.File.CreatedAt,
                                         Link = f.File.Link,
                                         LastModified = f.File.LastModified,
                                         SpaceId = s.Key.Id,
                                         Author = new AuthorDto() { Id = f.File.Owner.Id, GlobalId = f.File.Owner.GlobalId },
                                         CanRead = f.File.Space.Type == SpaceType.BinarySpace ?
                                             true : f.File.Space.Owner.GlobalId == userId ?
                                                 true : f.File.Owner.GlobalId == userId ?
                                                    true : f.userCanRead ?
                                                        true : f.roleCanRead ?
                                                            true : false,
                                         CanModify = f.File.Space.Type == SpaceType.BinarySpace ?
                                             true : f.File.Space.Owner.GlobalId == userId ?
                                                true : f.File.Owner.GlobalId == userId ?
                                                     true : f.userCanModify ?
                                                        true : f.roleCanModify ?
                                                            true : false
                                     })
                                 }).ToListAsync();

            var owners = (await _usersService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });
            foreach (var item in result)
            {
                Parallel.ForEach(item.Files, file =>
                {
                    file.Author.Name = owners.FirstOrDefault(o => o.Id == file.Author.GlobalId)?.Name;
                });
            }
            return result;
        }

        public async Task<ICollection<AppDto>> SearchFiles(FileType fileType, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return await FilterApp(fileType);
            }
            else
            {
                string userId = _usersService.CurrentUserId;

                var result = await (from f in _unitOfWork.Files.Query
                                    let userCanRead = f.Space.ReadPermittedUsers.Any(x => x.GlobalId == userId)
                                    let roleCanRead = f.Space.ReadPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                    let userCanModify = f.Space.ModifyPermittedUsers.Any(x => x.GlobalId == userId)
                                    let roleCanModify = f.Space.ModifyPermittedRoles.Any(x => x.Users.Any(p => p.GlobalId == userId))
                                    where f.FileType == fileType && f.Name.ToLower().Contains(text.ToLower())
                                         && (f.Space.Type == SpaceType.BinarySpace
                                         || f.Space.Owner.GlobalId == userId
                                         || userCanRead || roleCanRead
                                         || userCanModify || roleCanModify)
                                    group new { File = f, userCanRead, roleCanRead, userCanModify, roleCanModify } by f.Space into s
                                    select new AppDto()
                                    {
                                        SpaceId = s.Key.Id,
                                        SpaceType = s.Key.Type,
                                        Name = s.Key.Name,
                                        Files = s.Select(f => new FileUnitDto
                                        {
                                            Description = f.File.Description,
                                            FileType = f.File.FileType,
                                            Id = f.File.Id,
                                            IsDeleted = f.File.IsDeleted,
                                            Name = f.File.Name,
                                            CreatedAt = f.File.CreatedAt,
                                            Link = f.File.Link,
                                            LastModified = f.File.LastModified,
                                            SpaceId = s.Key.Id,
                                            Author = new AuthorDto() { Id = f.File.Owner.Id, GlobalId = f.File.Owner.GlobalId },
                                            CanRead = f.File.Space.Type == SpaceType.BinarySpace ?
                                                true : f.File.Space.Owner.GlobalId == userId ?
                                                    true : f.File.Owner.GlobalId == userId ?
                                                       true : f.userCanRead ?
                                                           true : f.roleCanRead ?
                                                               true : false,
                                            CanModify = f.File.Space.Type == SpaceType.BinarySpace ?
                                                true : f.File.Space.Owner.GlobalId == userId ?
                                                   true : f.File.Owner.GlobalId == userId ?
                                                        true : f.userCanModify ?
                                                           true : f.roleCanModify ?
                                                               true : false
                                        })
                                    }).ToListAsync();

                var owners = (await _usersService.GetAllAsync()).Select(f => new { Id = f.id, Name = f.name });
                foreach (var item in result)
                {
                    Parallel.ForEach(item.Files, file =>
                    {
                        file.Author.Name = owners.FirstOrDefault(o => o.Id == file.Author.GlobalId)?.Name;
                    });
                }
                return result;
            }
        }

        public async Task<string> UploadFile(HttpPostedFile file, AdditionalData fileData, int spaceId, int parentId)
        {
            _logger.WriteInfo("Upload method started");
            var user = await _usersService.GetCurrentUser();
            var localUser = await _unitOfWork?.Users?.Query.FirstOrDefaultAsync(x => x.GlobalId == user.id);

            var space = await _unitOfWork?.Spaces?.GetByIdAsync(spaceId);
            var parentFolder = await _unitOfWork?.Folders.GetByIdAsync(parentId);

            List<User> ReadPermittedUsers = new List<User>();

            ReadPermittedUsers.Add(localUser);

            List<User> ModifyPermittedUsers = new List<User>();

            ModifyPermittedUsers.Add(localUser);
            _logger.WriteInfo("Getting file info");
            string filename = fileData.Name + fileData.Extension;
            string mimeType = GetMimeType(filename);
            var isImage = IsImageMime(mimeType);
            _logger.WriteInfo("File name: " + filename + ", Mime type: " + mimeType);

            // File's content.
            Stream filestream = file.InputStream;
            byte[] byteArray = ReadFully(filestream);
            MemoryStream stream = new MemoryStream(byteArray);
            _logger.WriteInfo("File content stream: " + stream.Length);
            DriveService service;
            try
            {
                _logger.WriteInfo("Authorization started");
                service = AuthorizationService.ServiceAccountAuthorization();
                _logger.WriteInfo("Authorization succeeded");
            }
            catch (Exception e)
            {
                throw new Exception("Failed to authorize Drive service " + e.Message);
            }
            
            Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File();
            body.Name = filename;
            body.MimeType = mimeType;
            body.Parents = null;
            _logger.WriteInfo("File body created: " + body.Name);

            if (isImage)
            {
                try
                {
                    var image_link = await UploadToDriveAsync(service, body, stream, mimeType);
                    var prev_filename = "prev_" + fileData.Name + ".jpeg";
                    body.Name = prev_filename;
                    body.MimeType = "image/jpeg";
                    var img_stream = MakeThumbnail(stream);
                    var prev_link = await UploadToDriveAsync(service, body, img_stream, "image/jpeg");

                    var imageDto = new ImageUnit()
                    {
                        Name = filename,
                        FileType = FileType.Images,
                        Link = image_link,
                        Prev_Link = prev_link,
                        Description = fileData.Description,
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now,
                        IsDeleted = false,
                        Space = space,
                        FolderUnit = parentFolder,
                        Owner = await _unitOfWork?.Users?.Query.FirstOrDefaultAsync(u => u.GlobalId == user.id),
                        ReadPermittedUsers = ReadPermittedUsers,
                        ModifyPermittedUsers = ModifyPermittedUsers
                    };
                    _unitOfWork?.Files?.Create(imageDto);
                    await _unitOfWork?.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                string link = "";
                try
                {
                    _logger.WriteInfo("Begin upload");
                    link = await UploadToDriveAsync(service, body, stream, mimeType);
                    _logger.WriteInfo("Upload completed");
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to upload file to drive " + e.Message);
                }
                try
                {
                    var fileDto = new FileUnit()
                    {
                        Name = filename,
                        FileType = FileType.Uploaded,
                        Link = link,
                        Description = fileData.Description,
                        CreatedAt = DateTime.Now,
                        LastModified = DateTime.Now,
                        IsDeleted = false,
                        Space = space,
                        FolderUnit = parentFolder,
                        Owner = await _unitOfWork?.Users?.Query.FirstOrDefaultAsync(u => u.GlobalId == user.id),
                        ReadPermittedUsers = ReadPermittedUsers,
                        ModifyPermittedUsers = ModifyPermittedUsers
                    };
                    _unitOfWork?.Files?.Create(fileDto);
                    await _unitOfWork?.SaveChangesAsync();
                    _logger.WriteInfo("File saved to database");
                }
                catch (Exception e)
                {
                    _logger.WriteError(e, e.Message);
                    throw new Exception("Failed to save file to data base " + e.Message);
                }
            }
            return "Files uploaded successfully";
        }

        public async Task<DownloadFileDto> DownloadFile(string fileId)
        {
            DriveService service;
            try
            {
                service = AuthorizationService.ServiceAccountAuthorization();
            }
            catch (Exception e)
            {
                throw e;
            }
            var request = service.Files.Get(fileId);
            Google.Apis.Drive.v3.Data.File file = service.Files.Get(fileId).Execute();

            var stream = new System.IO.MemoryStream();
            await request.DownloadAsync(stream);

            stream.Seek(0, SeekOrigin.Begin);
            DownloadFileDto dto = new DownloadFileDto()
            {
                Name = file.Name,
                Type = file.MimeType,
                Content = stream
            };

            return dto;
        }

        public void Dispose()
        {
            _unitOfWork?.Dispose();
        }

        private async Task<string> UploadToDriveAsync(DriveService service, Google.Apis.Drive.v3.Data.File body, MemoryStream stream, string mimeType)
        {
            string result = "";
            try
            {
                _logger.WriteInfo("Creating upload request ");
                FilesResource.CreateMediaUpload request = service.Files.Create(body, stream, mimeType);
                _logger.WriteInfo("Uploading file to google drive");
                await request.UploadAsync();
                //_logger.WriteError(ex, "Drive exception " + ex.Message);
                result = request.ResponseBody.Id;
                _logger.WriteInfo("Result file id: " + result);
            }
            catch (Exception e)
            {
                _logger.WriteError(e, e.Message);
                throw e;
            }
            return result;
        }
        private bool IsImageMime(string mimeType)
        {
            string[] imageMimes = { "image/gif", "image/jpeg", "image/png", "image/tiff", "image/bmp" };
            if (imageMimes.Contains(mimeType))
                return true;
            else return false;
        }
        private MemoryStream MakeThumbnail(MemoryStream myImage)
        {
            MemoryStream ms = new MemoryStream();
            var image = Image.FromStream(myImage);
            var width = (int)(image.Width * 0.4);
            var height = (int)(image.Height * 0.4);
            using (Image thumbnail = Image.FromStream(myImage).GetThumbnailImage(width, height, null, new IntPtr()))
            {
                thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                return ms;
            }
        }
        private string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }
        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}