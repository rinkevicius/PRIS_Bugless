﻿using PRIS.Core.Library;
using PRIS.Web.Models.DbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRIS.Web.Models.Entity
{
    public class Exam : EntityBase
    {
        public int CityId { get; set; }
        public DateTime Date { get; set; }
        public double Task1_1 { get; set; }
        public double Task1_2 { get; set; }
        public double Task1_3 { get; set; }
        public double Task2_1 { get; set; }
        public double Task2_2 { get; set; }
        public double Task2_3 { get; set; }
        public double Task3_1 { get; set; }
        public double Task3_2 { get; set; }
        public double Task3_3 { get; set; }
        public double Task3_4 { get; set; }
        public string Comment { get; set; }

        public List<Result> Results { get; set; }
        public List<City> Cities { get; set; }
    }
}