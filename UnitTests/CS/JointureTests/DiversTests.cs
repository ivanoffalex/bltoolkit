﻿#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BLToolkit.Data;
using BLToolkit.Data.Linq;
using BLToolkit.DataAccess;
using BLToolkit.Mapping;
using NUnit.Framework;
using Newtonsoft.Json;
using PitagorDataAccess.Mappings.DataEntry;
using UnitTests.CS.JointureTests.Factories;
using UnitTests.CS.JointureTests.Mappings;
using UnitTests.CS.JointureTests.Tools;

#endregion

namespace UnitTests.CS.JointureTests
{
    [TestFixture]
    public abstract partial class AllTests
    {
        private void GetMediaSetting(DbManager db)
        {
            var query = from m in db.GetTable<DataMedia2>()
                        join s in db.GetTable<DataMediaSetting>() on m.IdMedia equals s.IdMedia
                        where m.IdLanguageData == 33 && s.IdLanguageDataI == 33
                        orderby m.Media
                        select
                            new
                                {
                                    s.Activation,
                                    m.IdMedia,
                                    m.Media,
                                    s.CaptureCode
                                };

            if (!true)
                query = query.Where(r => r.Activation < 10);

            var res = query.ToList();

            var mediae = res.Select(r => new Media
                {
                    ID_MEDIA = r.IdMedia,
                    MEDIA = r.Media,
                    CaptureCode = r.CaptureCode,
                    IsActivate = r.Activation < 10,
                }).ToList();
        }

        private void GetMediaLinq()
        {
            var all = new List<long> {21, 24, 25, 27, 38, 221};

            using (var db = ConnectionFactory.CreateDbManager())
            {
                using (var a = new ExecTimeInfo())
                {
                    var query = from m in db.GetTable<Media>()
                                join basicMedia in db.GetTable<BasicMedia>() on m.ID_BASIC_MEDIA equals
                                    basicMedia.ID_BASIC_MEDIA
                                where m.ACTIVATION == 0 && basicMedia.ACTIVATION == 0 && all.Contains(basicMedia.ID_CATEGORY)
                                select m;
                    var allMedia = query.ToList();
                }
                using (var a = new ExecTimeInfo())
                {
                    var query = from m in db.GetTable<Media>()
                                join basicMedia in db.GetTable<BasicMedia>() on m.ID_BASIC_MEDIA equals
                                    basicMedia.ID_BASIC_MEDIA
                                where m.ACTIVATION == 0 && basicMedia.ACTIVATION == 0 && all.Contains(basicMedia.ID_CATEGORY)
                                select m;
                    var allMedia = query.ToList();
                }

                using (var a = new ExecTimeInfo())
                {
                    var query = from basicMedia in db.GetTable<BasicMedia>()
                                where basicMedia.ACTIVATION == 0
                                select basicMedia;
                    var allMedia = query.ToList();
                }
                using (var a = new ExecTimeInfo())
                {
                    var query = from basicMedia in db.GetTable<BasicMedia>()
                                where basicMedia.ACTIVATION == 0
                                select basicMedia;
                    var allMedia = query.ToList();
                }
            }
        }

        private void GetMediaReq(string req)
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                using (var a = new ExecTimeInfo())
                {
                    db.SetCommand(req);
                    var allMedia = db.ExecuteList<Media>();
                }
                using (var a = new ExecTimeInfo())
                {
                    db.SetCommand(req);
                    var allMedia = db.ExecuteList<Media>();
                }
            }
        }

        [Test]
        public void GenericUserQueryTrueSelectError()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                int mediaId = 10;

                db.UseQueryText = true;
                var queryDataMediaFrance5 = db.GetTable<DataMedia>()
                    .Where(d => d.Date <= DateTime.Now && d.Date >= DateTime.Now && d.MediaId == mediaId)
                    .Select(d => d);

                var res = queryDataMediaFrance5.ToList();
                Console.WriteLine(res);
            }
        }

        [Test]
        public void GenericUserQueryTrueUpdateError()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.UseQueryText = true;

                db.BeginTransaction();

                int mediaId = 10;

                var query = from d in db.GetTable<MultimediaContext>()
                    where d.DateParution >= 100 &&
                          d.DateParution < 200 &&
                          d.MediaId == mediaId
                    select d;

                var updateCount = query.Set(d => d.MediaId, mediaId)
                    .Update();

                //var updateCount = db.GetTable<MultimediaContext>().
                //    Where(d =>
                //        d.DateParution >= 100 &&
                //        d.DateParution < 200 &&
                //        d.MediaId == mediaId
                //        )
                //    .Set(d => d.MediaId, mediaId)
                //    .Update();

                db.RollbackTransaction();
                Console.WriteLine(updateCount);
            }
        }

        [Test]
        public void ComplexJoin()
        {
            using (var pitagorDb = ConnectionFactory.CreateDbManager())
            {
                var groupsToExclude = new List<long> {549};
                var query = from m in pitagorDb.GetTable<MULTIMEDIA_TABLE>()
                            join p in pitagorDb.GetTable<PRODUCT>() on m.ID_PRODUCT equals p.Id
                            join s in pitagorDb.GetTable<SEGMENT>() on p.SegmentId equals s.Id
                            where m.DATE_CREATION >= new DateTime(2013, 07, 01) && m.DATE_CREATION < new DateTime(2013, 08, 01)
                                    && m.ID_CATEGORY_MULTIMEDIA == 95
                                    && m.ID_VEHICLE_I == 2
                                    && m.ACTIVATION == 0
                                    && !groupsToExclude.Contains(s.GroupId)
                            group m by new
                                {
                                    QualiStatus = m.STATUS_QUALI,
                                    Date = m.DATE_CREATION.Date
                                }
                            into b
                            select new {QualiStatusByDate = b.Key, Count = b.Count()};

                var res = query.ToList();

                Console.WriteLine(res);
            }
        }

        [Test]
        public void ComplexJoin2()
        {
            // BLToolkit BUG
            using (var pitagorDb = ConnectionFactory.CreateDbManager())
            {
                var groupsToExclude = new List<long> { 549 };
                var query = from m in pitagorDb.GetTable<MULTIMEDIA_TABLE>()
                            join p in pitagorDb.GetTable<PRODUCT>() on m.ID_PRODUCT equals p.Id
                            where p.ID_LANGUAGE_DATA == 33
                            join s in pitagorDb.GetTable<SEGMENT>() on p.SegmentId equals s.Id
                            where s.ID_LANGUAGE_DATA == 33
                            where m.DATE_CREATION >= new DateTime(2013, 07, 01) && m.DATE_CREATION < new DateTime(2013, 08, 01)
                                  && m.ID_CATEGORY_MULTIMEDIA == 95
                                  && m.ID_VEHICLE_I == 2
                                  && m.ACTIVATION == 0
                                  && groupsToExclude.Contains(s.GroupId)
                            select m.ID_MULTIMEDIA;

                var query2 = from m in pitagorDb.GetTable<MULTIMEDIA_TABLE>()
                             join em in query on m.ID_MULTIMEDIA equals em into m_em
                             from em in m_em.DefaultIfEmpty()
                             where m.DATE_CREATION >= new DateTime(2013, 07, 01) && m.DATE_CREATION < new DateTime(2013, 08, 01)
                                   && m.ID_CATEGORY_MULTIMEDIA == 95
                                   && m.ID_VEHICLE_I == 2
                                   && m.ACTIVATION == 0
                                   && em == null
                             group m by new
                                {
                                    QualiStatus = m.STATUS_QUALI,
                                    Date = m.DATE_CREATION.Date
                                }
                            into b
                            select new {QualiStatusByDate = b.Key, Count = b.Count()};

                var res = query2.ToList();

                Console.WriteLine(res);
            }
        }

        [Test]
        public void ComplexSelect()
        {
//select id_distributor, package_number,
//( select count(*)
//                from MEDIADISC01.ALBUM_FOLLOWED  a
//                where f.id_distributor = a.id_distributor and f.package_number = a.package_number ) as Total,
//( select count(*)
//                from MEDIADISC01.ALBUM_FOLLOWED  a
//                where f.id_distributor = a.id_distributor and f.package_number = a.package_number and a.date_numerisation is not null ) as Done
//from (
//                select distinct id_distributor, package_number
//                from MEDIADISC01.ALBUM_FOLLOWED
//                where date_numerisation is null
//) f

            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query2 = from m in db.GetTable<ALBUM_FOLLOWED>()
                             group m by new {m.ID_DISTRIBUTOR, m.PACKAGE_NUMBER}
                             into gn
                             select new
                                 {
                                     gn.Key.ID_DISTRIBUTOR,
                                     gn.Key.PACKAGE_NUMBER,
                                     Done = gn.Count(e => e.DATE_NUMERISATION != null),
                                     //Total = gn.Count(),
                                 };

                var res = query2.ToList();
                Console.WriteLine(res);
            }

            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from m in db.GetTable<ALBUM_FOLLOWED>()
                            where m.DATE_NUMERISATION == null
                            select new
                                {
                                    m.ID_DISTRIBUTOR,
                                    m.PACKAGE_NUMBER
                                };

                query = query.Distinct();

                var query2 = from m in query
                             join n in db.GetTable<ALBUM_FOLLOWED>() on new {m.ID_DISTRIBUTOR, m.PACKAGE_NUMBER} equals new {n.ID_DISTRIBUTOR, n.PACKAGE_NUMBER}
                             group n by new {n.ID_DISTRIBUTOR, n.PACKAGE_NUMBER}
                             into gn
                             select new
                                 {
                                     gn.Key.ID_DISTRIBUTOR,
                                     gn.Key.PACKAGE_NUMBER,
                                     Total = gn.Count()
                                 };

                var query3 = from m in query
                             join n in db.GetTable<ALBUM_FOLLOWED>() on new {m.ID_DISTRIBUTOR, m.PACKAGE_NUMBER} equals new {n.ID_DISTRIBUTOR, n.PACKAGE_NUMBER}
                             where n.DATE_NUMERISATION != null
                             group n by new {n.ID_DISTRIBUTOR, n.PACKAGE_NUMBER}
                             into gn
                             select new
                                 {
                                     gn.Key.ID_DISTRIBUTOR,
                                     gn.Key.PACKAGE_NUMBER,
                                     Done = gn.Count()
                                 };

                var query4 = from m in query2
                             join n in query3 on new {m.ID_DISTRIBUTOR, m.PACKAGE_NUMBER} equals new {n.ID_DISTRIBUTOR, n.PACKAGE_NUMBER} into mn
                             from n in mn.DefaultIfEmpty()
                             select new
                                 {
                                     m.ID_DISTRIBUTOR,
                                     m.PACKAGE_NUMBER,
                                     m.Total,
                                     n.Done
                                 };

                var res = query4.ToList();

                Console.WriteLine(res.Count);
            }
        }

        [Test]
        public void GroupByTest()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from m in db.GetTable<MULTIMEDIA_DB>()
                            join dv in db.GetTable<DATA_VERSION>() on m.ID_MULTIMEDIA equals dv.ID_MULTIMEDIA into m_dv
                            from dv in m_dv.DefaultIfEmpty()
                            group m by m.ID_MULTIMEDIA
                            into gm
                            where gm.Count() == 0
                            select new {Id = gm.Key, Count = gm.Count()};

                var res = query.ToList();

                Console.WriteLine(res.Count);
            }
        }

        [Test]
        public void GroupByTest2()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from m in db.GetTable<MULTIMEDIA_DB>()
                            join dv in db.GetTable<DATA_VERSION>() on m.ID_MULTIMEDIA equals dv.ID_MULTIMEDIA into m_dv
                            from dv in m_dv.DefaultIfEmpty()
                            where dv == null
                            group m by m.ID_MULTIMEDIA
                                into gm
                                select new { Id = gm.Key, Count = gm.Count() };

                var res = query.ToList();

                Console.WriteLine(res.Count);
            }
        }

        [Test]
        public void InsertArtistWithAutoSequence()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = new SqlQuery(db);
                var artist = new Label {Name = "TEST", DATE_CREATION = DateTime.Now, DATE_MODIFICATION = DateTime.Now, ACTIVATION = 10, ID_USER_ = 200};

                query.InsertWithIdentity(new DataImport
                    {
                        Commentary = "aaaa",
                        DeclaredProduct = "ssfsfsfsfsfsf",
                        IdMedia = 2024,
                        DeclaredId = 1
                    });
            }
        }

        [Test]
        public void InsertBlob()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var sqlQuery = new SqlQuery(db);

                var session = new static_nav_session
                    {
                        PDF_USER_FILENAME = "PDF_USER_FILENAME",
                        ID_LOGIN = 1,
                        STATUS = 3,
                        PDF_NAME = "COCO",
                        ID_PDF_RESULT_TYPE = 29,
                        STATIC_NAV_SESSION = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new DataMedia2 {Activation = 2, IdLanguageData = 123, IdMedia = 2002, Media = "COCO"}))
                    };

                var sessionId = sqlQuery.InsertWithIdentity(session);
                Console.WriteLine(sessionId);
            }
        }

        [Test]
        public void InsertDataRadio()
        {
            using (new ExecTimeInfo())
            {
                using (var db = ConnectionFactory.CreateDbManager())
                {
                    db.BeginTransaction();

                    try
                    {
                        var baseData = db.GetTable<DATA_RADIO>()
                                         .Single(d => d.ID_DATA_VERSION == 242000147724 &&
                                                      d.IdCobAdvert == 1);

                        baseData.ID_DATA_RADIO = 0; // Optional
                        baseData.IdCobAdvert = 3;
                        baseData.ID_PRODUCT = 180000;
                        baseData.DATE_MODIFICATION = DateTime.Now;

                        var query = new SqlQuery(db);
                        var count = (long) query.InsertWithIdentity(baseData);
                        Console.WriteLine(count + " -> " + db.LastQuery);
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    finally
                    {
                        db.RollbackTransaction();
                    }
                }
            }
        }

        [Test]
        public void InsertNewArtist()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.UseQueryText = true;
                db.BeginTransaction();

                //var query = new SqlQuery<artist>(db);
                //var newId = (long)query.InsertWithIdentity(new artist
                //    {
                //        ARTIST = "Artist",
                //        ARTIST_LIKE = "test",
                //        DATE_CREATION = DateTime.Now,
                //        DATE_MODIFICATION = DateTime.Now,
                //        DATE_CONFIRMED = new DateTime(2070, 1, 1),
                //        ID_USER_ = 52,
                //        EXT_ARTIST_ID = 0,
                //        ACTIVATION = 0,
                //    });

                //db.RollbackTransaction();

                //Console.WriteLine(newId);

                var idArtist = (long) db.GetTable<artist>().InsertWithIdentity(() =>
                                                                               new artist
                                                                                   {
                                                                                       ARTIST = "Artist",
                                                                                       ARTIST_LIKE = "test",
                                                                                       DATE_CREATION = DateTime.Now,
                                                                                       DATE_MODIFICATION = DateTime.Now,
                                                                                       DATE_CONFIRMED = new DateTime(2070, 1, 1),
                                                                                       ID_USER_ = 52,
                                                                                       EXT_ARTIST_ID = 0,
                                                                                       ACTIVATION = 0,
                                                                                   });

                db.RollbackTransaction();

                Console.WriteLine(idArtist);
            }
        }

        [Test]
        public void InsertNewTrack()
        {
            //foreach (var d in Enum.GetValues(typeof (DbType)))
            //{
            //    var p = new OracleParameter();
            //    try
            //    {
            //        // Since the OracleDbType is inferred when the DbType is set
            //        // setting DbType for an OracleParameter then displaying
            //        // the resulting OracleDbType will show the mapping for valid values.
            //        Console.Write("{0} => ", d);
            //        p.DbType = (DbType) d;
            //        Console.WriteLine("{0}", p.OracleDbType.ToString());
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}

            using (var db = ConnectionFactory.CreateDbManager())
            {
                //db.UseQueryText = true;
                db.BeginTransaction();

                var query = new SqlQuery(db);
                //var id = (long) query.InsertWithIdentity(
                var id = db.GetTable<track>().InsertWithIdentity(() =>
                                                                 new track
                                                                     {
                                                                         TRACK = "title",
                                                                         TRACK_LIKE = "titleLike",
                                                                         ACTIVATION = 0,
                                                                         DATE_CREATION = DateTime.Now,
                                                                         DATE_MODIFICATION = DateTime.Now,
                                                                         DATE_RELEASE = null,
                                                                         ID_GENRE = 1,
                                                                         ID_LABEL = 1,
                                                                         ID_COUNTRY_CODE = "ww",
                                                                         ID_USER_ = 52,
                                                                         ID_ARTIST = 104973,
                                                                         VALIDITY_STATUS = 64,
                                                                         DURATION = 210
                                                                     });
                // TODO Why ID is decimal? Is it because of the sequence return type?

                db.RollbackTransaction();

                Console.WriteLine(id);
            }
        }

        [Test]
        public void InsertWithIdentity()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                var t = db.GetTable<RECO_RADIO>();
                var now = DateTime.Now.AddDays(30);
                var id = t.InsertWithIdentity(
                    () =>
                    new RECO_RADIO
                        {
                            ACTIVATION = 0,
                            COMMENTARY = string.Empty,
                            DATE_CREATION = DateTime.Now,
                            DATE_LAST_IMPORT = DateTime.Today,
                            DATE_MEDIA = DateTime.Today,
                            DATE_MODIFICATION = DateTime.Now,
                            ID_LANGUAGE_DATA_I = 33,
                            ID_MEDIA = 2001,
                            ID_MULTIMEDIA_FILE = 463413,
                            IdMultVal = 0,
                            INPUT_STATUS = 0,
                            RATE = 0,
                            TAG_DURATION = 20,
                            TAG_MATCH_BEGINNING = 0,
                            TAG_MATCH_DURATION = 20,
                            TIME_MEDIA = now
                        });
                Console.WriteLine(id + " : " + id.GetType() + " ->" + db.LastQuery);

                db.RollbackTransaction();
            }
        }

        [Test]
        public void InsertWithIdentityDbManager()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                var dataProduct = new DataProductPending
                    {
                        Activation = 0,
                        Commentary = "Valeriu",
                        DateCreation = DateTime.Now,
                        DateModification = DateTime.Now,
                        ProductPending = "Valeriu",
                        IdUserCreate = 0,
                        UserProgram = "Valeriu"
                    };

                var res = db.InsertWithIdentity(dataProduct);

                db.RollbackTransaction();

                Console.WriteLine(res);
                Console.WriteLine(db.LastQuery);
            }
        }

        [Test]
        public void InsertWithIdentityLinq()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                var t = db.GetTable<DataProductPending>();
                var now = DateTime.Now.AddDays(30);
                var res = t.InsertWithIdentity(
                    () =>
                    new DataProductPending
                        {
                            Activation = 0,
                            Commentary = "Valeriu",
                            DateCreation = DateTime.Now,
                            DateModification = DateTime.Now,
                            ProductPending = "Valeriu",
                            IdUserCreate = 0,
                            UserProgram = "Valeriu"
                        });


                db.RollbackTransaction();

                Console.WriteLine(res);
                Console.WriteLine(db.LastQuery);
            }
        }

        [Test]
        public void InsertWithIdentityQuery()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                var dataProduct = new DataProductPending
                    {
                        Activation = 0,
                        Commentary = "Valeriu",
                        DateCreation = DateTime.Now,
                        DateModification = DateTime.Now,
                        ProductPending = "Valeriu",
                        IdUserCreate = 0,
                        UserProgram = "Valeriu"
                    };

                var query = new SqlQuery(db);
                var res = query.InsertWithIdentity(dataProduct);

                db.RollbackTransaction();

                Console.WriteLine(res);
                Console.WriteLine(db.LastQuery);
            }
        }

        [Test]
        public void InsertWithIdentityQuery2()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                var dbKeyword = new Keyword
                    {
                        NAME = "Valeriu",
                        DATE_CREATION = DateTime.UtcNow,
                    };

                var query = new SqlQuery(db);
                var res = query.InsertWithIdentity(dbKeyword);

                db.RollbackTransaction();

                Console.WriteLine(res);
                Console.WriteLine(db.LastQuery);
            }
        }

        [Test]
        public void SelectBlob()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from n in db.GetTable<static_nav_session>()
                            where n.ID_STATIC_NAV_SESSION == 102959
                            select n;

                var res = query.ToList();

                var sqlQuery = new SqlQuery(db);
                var element = sqlQuery.SelectByKey<static_nav_session>(102959);

                //var blob = element.STATIC_NAV_SESSION;
                //byte[] buffer = new byte[blob.Length];
                //blob.Read(buffer, 0, buffer.Length);

                byte[] buffer = element.STATIC_NAV_SESSION;

                var dateMedia = JsonConvert.DeserializeObject<DataMedia>(Encoding.UTF8.GetString(buffer));
                Console.WriteLine(dateMedia);

                //MemoryStream memoryStream = new MemoryStream(buffer);
                //BinaryFormatter binaryFormatter = new BinaryFormatter();
                //var dede = binaryFormatter.Deserialize(memoryStream);
                //Console.WriteLine(dede);

                Console.WriteLine(element);

                Console.WriteLine(res.Count);
            }
        }

        [Test]
        public void SelectClobField()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from s in db.GetTable<FORM_SCRIPT>()
                            where s.DATE_CREATION > new DateTime(2012, 07, 01)
                            select s;

                var res = query.ToList();
                Assert.IsNotEmpty(res);
            }
        }

        [Test]
        public void SelectDataEntries()
        {
            using (new ExecTimeInfo())
            {
                var beginSpotPeriod = new DateTime(2012, 08, 09);
                var endSpotPeriod = new DateTime(2012, 08, 09);
                using (var pitagorDb = ConnectionFactory.CreateDbManager())
                {
                    var dbQuery = from dr in pitagorDb.GetTable<DATA_RADIO>()
                                  join dv in pitagorDb.GetTable<DATA_VERSION>() on dr.ID_DATA_VERSION equals
                                      dv.ID_DATA_VERSION
                                  where dr.DATE_MEDIA >= beginSpotPeriod && dr.DATE_MEDIA <= endSpotPeriod
                                        && dr.DATE_SPOT_BEGINNING >= beginSpotPeriod &&
                                        dr.DATE_SPOT_END <= endSpotPeriod
                                  select
                                      new DataEntryBroadcast
                                          {
                                              Id = dr.ID_DATA_VERSION,
                                              VersionId = dv.ID_MULTIMEDIA,
                                              DateMedia = dr.DATE_MEDIA,
                                              MediaId = dr.ID_MEDIA,
                                              SpotBegin = dr.DATE_SPOT_BEGINNING,
                                              SpotEnd = dr.DATE_SPOT_END,
                                          };

                    var res = dbQuery.ToList();
                    Console.WriteLine(res.Count);
                }
            }
        }

        [Test]
        public void SelectGetMediaSettingMultiple()
        {
            SimulateWork(GetMediaSetting, ConnectionFactory, 20, 1);
        }

        [Test]
        public void SelectInPeriod()
        {
            using (new ExecTimeInfo())
            {
                using (var db = ConnectionFactory.CreateDbManager())
                {
                    var beginMinute = 0;
                    var beginHour = 0;

                    var endMinute = 50;
                    var endHour = 23;

                    IQueryable<TitleQuery> queryTitle = from data in db.GetTable<DataMusic>()
                                                        join title in db.GetTable<Title>() on data.TitleId equals title.Id
                                                        join artist in db.GetTable<Artist>() on title.ArtistID equals artist.Id
                                                        where data.MediaId == 2002 &&
                                                              data.DateMedia >= DateTime.Today &&
                                                              data.DateMedia <= DateTime.Today &&
                                                              data.DateSpot.TimeOfDay >= new TimeSpan(beginHour, beginMinute, 0) &&
                                                              data.DateSpot.TimeOfDay <= new TimeSpan(endHour, endMinute, 0) &&
                                                              data.DateMedia >= Sql.Date
                                                        select new TitleQuery
                                                            {
                                                                Title = title,
                                                                Artist = artist,
                                                            };
                    var res = queryTitle.ToList();
                    Console.WriteLine(res);
                }
            }
        }

        [Test]
        public void LeftJoin()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from m in db.GetTable<MULTIMEDIA_DB>()
                            join dv in db.GetTable<DATA_VERSION>() on m.ID_MULTIMEDIA equals dv.ID_MULTIMEDIA into m_dv
                            from dv in m_dv.DefaultIfEmpty()
                            where dv != null
                            select new {m, dv};

                var res = query.ToList();

                Console.WriteLine(res.Count);
            }
        }

        [Test]
        public void SelectTest()
        {
            using (new ExecTimeInfo())
            using (var db = ConnectionFactory.CreateDbManager())
            {
                IQueryable<Mappings.Mapping> resultG = from d in db.GetTable<DataMapping>()
                                                       where d.IdLocker == null && d.IdLanguage == 33
                                                       select new Mappings.Mapping
                                                           {
                                                               IdMapping = d.IdMapping,
                                                               DeclaredId = d.DeclaredId,
                                                               DeclaredProduct = d.DeclaredProduct,
                                                               MappingState = (MappingState) d.Activation,
                                                               Product = new Product
                                                                   {
                                                                       IdProduct =
                                                                           d.IdProduct == null
                                                                               ? 0
                                                                               : d.IdProduct.Value
                                                                   },
                                                               ProductPending = new ProductPending
                                                                   {
                                                                       IdProductPending =
                                                                           d.IdProductPending == null
                                                                               ? 0
                                                                               : d.IdProductPending.Value
                                                                   }
                                                           };

                var res = resultG.Distinct().OrderBy(p => p.IdMapping).ToList();
            }
        }

        [Test]
        public void SelectSqlTextOracleClobWithFullMappingTest1()
        {
            string sql = File.ReadAllText(@"c:\requeteOrqua.txt");

            using (var db = ConnectionFactory.CreateDbManager())
            {
                using (var a = new ExecTimeInfo())
                {
                    //db.SetCommand(sql);
                    //var reader = db.ExecuteReader();
                    //var schemaTable = reader.GetSchemaTable();
                    //while (reader.Read())
                    //{
                    //    //var values = new object[schemaTable.Columns.Count];
                    //    //reader.GetValues(values);
                    //    reader.GetValue(0);
                    //}

                    DbManager dbCmd = db.SetCommand(sql);
                    dbCmd.MappingSchema = new FullMappingSchema(db);
                    var allMedia = dbCmd.ExecuteList<FILE_FORM>();

                    foreach (FILE_FORM fileForm in allMedia)
                    {
                        if (fileForm.SCRIPT_FIELD != null && !fileForm.SCRIPT_FIELD.IsNull)
                        {
                            //string clobValue = fileForm.SCRIPT_FIELD.Value;
                        }
                    }
                }
            }
        }

        [Test]
        public void ContainsExactlyExtension()
        {
            Console.WriteLine("Hello1 Hello2".ContainsExactly("Hello2"));

            Console.WriteLine(Math.Round(0.5));

            var creationDate = new DateTime(2012, 1, 1);
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from s in db.GetTable<SCRIPT_TABLE>()
                            where s.DATE_CREATION > creationDate && s.SCRIPT.ContainsExactly("station") > 0
                            select s;

                var res = query.ToList();
                Console.WriteLine(res.Count);
                Console.WriteLine(db.LastQuery);
            }
        }

        [Test]
        public void SelectTitleFull()
        {
            string req = " select m.ID_MEDIA, m.ID_BASIC_MEDIA, m.ACTIVATION from PITAFR01.MEDIA m " +
                         " inner join PITAFR01.BASIC_MEDIA bm on m.ID_BASIC_MEDIA  = bm.ID_BASIC_MEDIA " +
                         " where m.ACTIVATION = 0 and bm.ACTIVATION = 0 and bm.ID_CATEGORY IN (21, 24, 25, 27, 38, 221) ";

            GetMediaReq(req);
            GetMediaReq(req);


            Console.WriteLine("------------------------------------------------------------------------------");

            GetMediaLinq();
            GetMediaLinq();
        }

        [Test]
        public void SelectTooLong()
        {
            using (new ExecTimeInfo())
            {
                using (var db = ConnectionFactory.CreateDbManager())
                {
                    var query = from dt in db.GetTable<DataDeclarativeTrack>()
                                join dd in db.GetTable<DataDeclarativeData>() on dt.IdDeclarativeTrack equals dd.IdDeclarativeTrack
                                join ms in db.GetTable<MediaSetting>() on dd.IdMedia equals ms.IdMedia
                                where dt.Status == (short) DeclarativeTitleStatus.Default ||
                                      dt.Status == (short) DeclarativeTitleStatus.Locked
                                select new {dd.DateMedia, dt.IdDeclarativeTrack, ms.Activation};

                    query = false
                                ? query.Where(e => e.Activation == ActivationMedia.Priority)
                                : query.Where(e => e.Activation == ActivationMedia.Default);

                    var res = query.Distinct().ToList();
                    Assert.IsNotEmpty(res);
                }
            }
        }

        [Test]
        public void SelectTooLong2()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.SetCommand(File.ReadAllText(@"c:\requete2.txt"));
                var res = db.ExecuteList<Monitoring>();

                Assert.IsNotEmpty(res);
            }
        }

        [Test]
        public void SelectWithManyParameters()
        {
            var productIds = new[]
                {
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9,
                    10,
                    11,
                    12,
                    13,
                    14,
                    15,
                    16,
                    17,
                    18,
                }.ToList();
            var duration = TimeSpan.FromSeconds(3);

            using (var manager = ConnectionFactory.CreateDbManager())
            {
                manager.UseQueryText = true;

                var queryMultimedia = from multimedia in manager.GetTable<Multimedia>()
                                      join multimediaFile in manager.GetTable<MultimediaFile>() on multimedia.Id equals multimediaFile.MultimediaId
                                          into joinedMultimediaFile
                                      from subMultimediaFile in joinedMultimediaFile.DefaultIfEmpty()
                                      join multimediaCobranding1 in manager.GetTable<MultimediaCobranding>() on new {MultimediaId = multimedia.Id, multimedia.CategoryMultimediaId} equals new {multimediaCobranding1.MultimediaId, multimediaCobranding1.CategoryMultimediaId}
                                      where multimedia.CategoryMultimediaId == 65 &&
                                            multimediaCobranding1.ProductId == productIds[0] &&
                                            multimediaCobranding1.CategoryMultimediaId == 65
                                      select new
                                          {
                                              Multimedia = multimedia,
                                              MultimediaFile = subMultimediaFile
                                          };

                for (int i = 1; i < productIds.Count; i++)
                {
                    var productId = productIds[i];
                    queryMultimedia = from productMultimedia in queryMultimedia
                                      join multimediaCobranding1 in
                                          manager.GetTable<MultimediaCobranding>()
                                                 .Where(mc => mc.CategoryMultimediaId == 65) on productMultimedia.Multimedia.Id equals
                                          multimediaCobranding1.MultimediaId
                                      where multimediaCobranding1.ProductId == productId &&
                                            multimediaCobranding1.CategoryMultimediaId == 65
                                      select productMultimedia;
                }

                queryMultimedia = queryMultimedia.Where(p => p.Multimedia.DurationInSeconds == (long) duration.TotalSeconds);

                var tempMultimediae = queryMultimedia.Distinct().ToList();
                Console.WriteLine(tempMultimediae);
            }
        }

        [Test]
        public void SelectWithManyParametersPSharpCharacter()
        {
            using (var manager = ConnectionFactory.CreateDbManager())
            {
                int prodId = 203032;
                manager.UseQueryText = false;
                var query = from p in manager.GetTable<CProduct>()
                            where p.Id == prodId &&
                                  p.Label.EndsWith("P#") &&
                                  p.LanguageId == 33 &&
                                  p.Activation < 50
                            select p;

                var product = query.FirstOrDefault();
                Console.WriteLine(product);
            }
        }

        [Test]
        public void TestGetDataRadioWithTimeSpan()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from m in db.GetTable<DATA_VERSION>()
                            where m.DATE_CREATION.TimeOfDay > TimeSpan.FromHours(11)
                            select m;
                query = query.Take(5);
                var res = query.ToList();
                Assert.IsNotEmpty(res);
            }
        }

        [Test]
        public void TestSlowUpdate()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                var beginSpot = new DateTime(2008, 08, 01, 6, 33, 31, DateTimeKind.Local);

                db.GetTable<Test_Query_Update_Duration.DbTypes.DataMusic>().Where(dm => dm.Id == 1459952)
                  .Set(dm => dm.DateSpot, beginSpot)
                  .Set(dm => dm.DurationInSeconds, 60)
                  .Set(dm => dm.UserId, 2)
                  .Set(dm => dm.ModifiedAt, DateTime.Now)
                  .Update();

                db.RollbackTransaction();
            }

            using (new ExecTimeInfo())
            {
                using (var db = ConnectionFactory.CreateDbManager())
                {
                    db.BeginTransaction();

                    var beginSpot = new DateTime(2008, 08, 01, 6, 33, 31, DateTimeKind.Local);

                    db.GetTable<Test_Query_Update_Duration.DbTypes.DataMusic>().Where(dm => dm.Id == 1459952)
                      .Set(dm => dm.DateSpot, beginSpot)
                      .Set(dm => dm.DurationInSeconds, 60)
                      .Set(dm => dm.UserId, 2)
                      .Set(dm => dm.ModifiedAt, DateTime.Now)
                      .Update();

                    db.RollbackTransaction();
                }
            }
        }

        [Test]
        public void TestUpdate()
        {
            using (var db = ConnectionFactory.CreateDbManager())
            {
                var query = from d in db.GetTable<DataProductPending>()
                            where d.IdProductPending == 1
                            select d;

                DataProductPending productPending = query.First();
                productPending.DateModification = DateTime.Now;

                var queryUpdate = new SqlQuery(db);
                queryUpdate.Update(productPending);

                Console.WriteLine(db.LastQuery);
            }
        }

        [Test]
        public void TestUpdateQueryWithJoin()
        {
            var Nat = new List<long> {68, 31, 35, 37, 39};
            var Tnt = new List<long> {44, 261, 301, 78};

            var W9D17France4 = new List<long> {3128, 14791, 3129};
            var LiveStations = new List<long> {3140, 3120, 3130, 3132, 3190, 3161, 3110, 3157, 3139, 3123, 3133, 3124, 3617, 14791, 3125, 3129, 3102, 3168, 3613, 3188, 3126, 3127, 3194, 3612, 3128, 19609, 18729};
            var LiveStationsNat = new List<long> {3140, 3120, 3130, 3132, 3190, 3161, 3110, 3157, 3139, 3123, 3133, 3124};
            List<long> LiveStationsTnt = LiveStations.Except(LiveStationsNat).ToList();

            using (var db = ConnectionFactory.CreateDbManager())
            {
                db.BeginTransaction();

                DateTime mardiDate = DateTime.Today.AddDays(-2);

                try
                {
                    var query = from p in db.GetTable<data_media>()
                                join m in db.GetTable<all_media>() on p.ID_MEDIA equals m.ID_MEDIA
                                where p.DATA_PRESENCE < 2 && p.DATE_MEDIA == mardiDate && LiveStationsTnt.Contains(p.ID_MEDIA)
                                      && Tnt.Contains(m.ID_CATEGORY)
                                select p;

                    var resQuery = query.ToList();

                    var modifiedLignes = query.Set(d => d.DATA_PRESENCE, () => 2)
                                              .Set(d => d.DATE_MODIFICATION, () => DateTime.Now)
                                              .Update();

                    Assert.IsTrue(resQuery.Count == modifiedLignes);

                    db.RollbackTransaction();
                }
                catch (Exception e)
                {
                    db.RollbackTransaction();
                    Console.WriteLine(e);
                }
            }
        }
    }
}