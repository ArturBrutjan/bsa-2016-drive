﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drive.DataAccess.Interfaces;

namespace Drive.WebHost.Services.Pro.Abstract
{
    public interface IBasicService<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetAsync(int id);
        Task<T> CreateAsync(T dto);
        Task<T> UpdateAsync(int id, T dto);
        Task DeleteAsync(int id);
        void Dispose();
    }
}
