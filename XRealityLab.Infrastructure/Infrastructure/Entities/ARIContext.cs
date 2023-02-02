using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Infrastructure.Entities
{
    public partial class ARIContext : DbContext
    {
        public ARIContext()
        {
        }

        public ARIContext(DbContextOptions<ARIContext> options)
            : base(options)
        {
        }

        public virtual DbSet<CompanyCar> CompanyCars { get; set; }
        public virtual DbSet<CompanyCarReservation> CompanyCarReservations { get; set; }
        public virtual DbSet<Complaint> Complaints { get; set; }
        public virtual DbSet<ComplaintSubject> ComplaintSubjects { get; set; }
        public virtual DbSet<Country> Countries { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<Customer1> Customers1 { get; set; }
        public virtual DbSet<Guest> Guests { get; set; }
        public virtual DbSet<GuestCompany> GuestCompanies { get; set; }
        public virtual DbSet<GuestUser> GuestUsers { get; set; }
        public virtual DbSet<Ingredient> Ingredients { get; set; }
        public virtual DbSet<Load> Loads { get; set; }
        public virtual DbSet<LoadOrder> LoadOrders { get; set; }
        public virtual DbSet<LoadOrderFilter> LoadOrderFilters { get; set; }
        public virtual DbSet<LoadOrderPallet> LoadOrderPallets { get; set; }
        public virtual DbSet<LoadPallet> LoadPallets { get; set; }
        public virtual DbSet<Material> Materials { get; set; }
        public virtual DbSet<Material1> Materials1 { get; set; }
        public virtual DbSet<MaterialItem> MaterialItems { get; set; }
        public virtual DbSet<MaterialLagerdaysExpirationdaysTemp> MaterialLagerdaysExpirationdaysTemps { get; set; }
        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<PackagingLineForm> PackagingLineForms { get; set; }
        public virtual DbSet<PackagingLineFormPallet> PackagingLineFormPallets { get; set; }
        public virtual DbSet<Pallet> Pallets { get; set; }
        public virtual DbSet<ProductFamily> ProductFamilies { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<SapA600> SapA600s { get; set; }
        public virtual DbSet<SapA601> SapA601s { get; set; }
        public virtual DbSet<SapA602> SapA602s { get; set; }
        public virtual DbSet<SapA603> SapA603s { get; set; }
        public virtual DbSet<SapA604> SapA604s { get; set; }
        public virtual DbSet<SapA605> SapA605s { get; set; }
        public virtual DbSet<SapA606> SapA606s { get; set; }
        public virtual DbSet<SapCustomerMaterialMasterKnmt> SapCustomerMaterialMasterKnmts { get; set; }
        public virtual DbSet<SapDataperwarehouseMlgn> SapDataperwarehouseMlgns { get; set; }
        public virtual DbSet<SapGeneralDataCustomerMaterKna1> SapGeneralDataCustomerMaterKna1s { get; set; }
        public virtual DbSet<SapGeneralMaterialDataMara> SapGeneralMaterialDataMaras { get; set; }
        public virtual DbSet<SapKonp> SapKonps { get; set; }
        public virtual DbSet<SapMast> SapMasts { get; set; }
        public virtual DbSet<SapMaterialDescriptionMakt> SapMaterialDescriptionMakts { get; set; }
        public virtual DbSet<SapStko> SapStkos { get; set; }
        public virtual DbSet<SapStpo> SapStpos { get; set; }
        public virtual DbSet<SapZgtinTable> SapZgtinTables { get; set; }
        public virtual DbSet<SapZmatinfo> SapZmatinfos { get; set; }
        public virtual DbSet<Truck> Trucks { get; set; }
        public virtual DbSet<TruckOrderPallet> TruckOrderPallets { get; set; }
        public virtual DbSet<UpdateFromSaplog> UpdateFromSaplogs { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Warehouse> Warehouses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=ari.database.windows.net, 1433;Database=ARI_DEV;User ID=ari;Password=Ruudvan10!;Integrated Security=False");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<CompanyCar>(entity =>
            {
                entity.ToTable("CompanyCar");

                entity.Property(e => e.CompanyCarId).HasColumnName("CompanyCarID");

                entity.Property(e => e.LicensePlate)
                    .IsRequired()
                    .HasMaxLength(15);

                entity.Property(e => e.Model)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<CompanyCarReservation>(entity =>
            {
                entity.ToTable("CompanyCarReservation");

                entity.Property(e => e.CompanyCarReservationId).HasColumnName("CompanyCarReservationID");

                entity.Property(e => e.ArrivalDateTime).HasColumnType("datetime");

                entity.Property(e => e.CompanyCarId).HasColumnName("CompanyCarID");

                entity.Property(e => e.DepartureDateTime).HasColumnType("datetime");

                entity.Property(e => e.MaintenanceType).HasMaxLength(50);

                entity.Property(e => e.Observations).HasMaxLength(50);

                entity.Property(e => e.RegistrationDateTime).HasColumnType("datetime");

                entity.Property(e => e.Sign).HasMaxLength(150);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.CompanyCar)
                    .WithMany(p => p.CompanyCarReservations)
                    .HasForeignKey(d => d.CompanyCarId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_CompanyCarReservation_CompanyCar");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CompanyCarReservations)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK_CompanyCarReservation_User");
            });

            modelBuilder.Entity<Complaint>(entity =>
            {
                entity.ToTable("Complaint");

                entity.Property(e => e.ComplaintId).HasColumnName("ComplaintID");

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.Documents).HasMaxLength(250);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.FullName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.IssuedDateTime).HasColumnType("datetime");

                entity.Property(e => e.Street).HasMaxLength(50);

                entity.Property(e => e.SubjectId).HasColumnName("SubjectID");

                entity.Property(e => e.Telephone).HasMaxLength(50);

                entity.HasOne(d => d.Subject)
                    .WithMany(p => p.Complaints)
                    .HasForeignKey(d => d.SubjectId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Complaint_ComplaintSubject1");
            });

            modelBuilder.Entity<ComplaintSubject>(entity =>
            {
                entity.ToTable("ComplaintSubject");

                entity.Property(e => e.ComplaintSubjectId)
                    .ValueGeneratedNever()
                    .HasColumnName("ComplaintSubjectID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<Country>(entity =>
            {
                entity.ToTable("Country");

                entity.Property(e => e.CountryId)
                    .ValueGeneratedNever()
                    .HasColumnName("CountryID");

                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.NameEl)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("NameEL");

                entity.Property(e => e.NameEn)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("NameEN");
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customer");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.CheckDigit).HasMaxLength(250);

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.CustomerSapid)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("CustomerSAPID");

                entity.Property(e => e.LocationNo1).HasMaxLength(20);

                entity.Property(e => e.LocationNo2).HasMaxLength(20);

                entity.Property(e => e.Name1)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name2).HasMaxLength(50);

                entity.Property(e => e.PostalCode).HasMaxLength(250);

                entity.Property(e => e.SearchTermsCode).HasMaxLength(20);

                entity.Property(e => e.Street).HasMaxLength(50);

                entity.Property(e => e.TaxNumber1).HasMaxLength(250);

                entity.Property(e => e.TaxNumber2).HasMaxLength(250);

                entity.Property(e => e.VatregNo)
                    .HasMaxLength(250)
                    .HasColumnName("VATRegNo");

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.Customers)
                    .HasForeignKey(d => d.CountryId)
                    .HasConstraintName("FK_Customer_Country_");
            });

            modelBuilder.Entity<Customer1>(entity =>
            {
                entity.HasKey(e => e.CustomerId)
                    .HasName("PK_Customer");

                entity.ToTable("Customer_");

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.Address).HasMaxLength(20);

                entity.Property(e => e.CheckDigit).HasMaxLength(10);

                entity.Property(e => e.City).HasMaxLength(50);

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.CustomerSapid)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("CustomerSAPID");

                entity.Property(e => e.LocationNo1).HasMaxLength(20);

                entity.Property(e => e.LocationNo2).HasMaxLength(20);

                entity.Property(e => e.Name1)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Name2).HasMaxLength(50);

                entity.Property(e => e.SearchTermsCode).HasMaxLength(20);

                entity.Property(e => e.Street).HasMaxLength(50);

                entity.Property(e => e.TaxNumber1).HasMaxLength(20);

                entity.Property(e => e.TaxNumber2).HasMaxLength(20);

                entity.Property(e => e.VatregNo)
                    .HasMaxLength(20)
                    .HasColumnName("VATRegNo");

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.Customer1s)
                    .HasForeignKey(d => d.CountryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Customer_Country");
            });

            modelBuilder.Entity<Guest>(entity =>
            {
                entity.ToTable("Guest");

                entity.Property(e => e.GuestId).HasColumnName("GuestID");

                entity.Property(e => e.ArrivalDateTime).HasColumnType("datetime");

                entity.Property(e => e.DepartureDateTime).HasColumnType("datetime");

                entity.Property(e => e.GuestCompanyId).HasColumnName("GuestCompanyID");

                entity.Property(e => e.GuestUserId).HasColumnName("GuestUserID");

                entity.Property(e => e.LicensePlate).HasMaxLength(15);

                entity.Property(e => e.Sign).HasMaxLength(250);

                entity.Property(e => e.Telephone).HasMaxLength(20);

                entity.Property(e => e.VisitationPurpose).HasMaxLength(250);

                entity.HasOne(d => d.GuestCompany)
                    .WithMany(p => p.Guests)
                    .HasForeignKey(d => d.GuestCompanyId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Guest_GuestCompany");

                entity.HasOne(d => d.GuestUser)
                    .WithMany(p => p.Guests)
                    .HasForeignKey(d => d.GuestUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Guest_GuestUser");
            });

            modelBuilder.Entity<GuestCompany>(entity =>
            {
                entity.ToTable("GuestCompany");

                entity.Property(e => e.GuestCompanyId).HasColumnName("GuestCompanyID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<GuestUser>(entity =>
            {
                entity.ToTable("GuestUser");

                entity.Property(e => e.GuestUserId).HasColumnName("GuestUserID");

                entity.Property(e => e.Firstname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Lastname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(15);
            });

            modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.ToTable("Ingredient");

                entity.Property(e => e.IngredientId)
                    .ValueGeneratedNever()
                    .HasColumnName("IngredientID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            modelBuilder.Entity<Load>(entity =>
            {
                entity.ToTable("Load");

                entity.Property(e => e.LoadId).HasColumnName("LoadID");

                entity.Property(e => e.Cmrdocuments)
                    .HasMaxLength(250)
                    .HasColumnName("CMRDocuments");

                entity.Property(e => e.CreationDateTime).HasColumnType("datetime");

                entity.Property(e => e.DriverDocuments).HasMaxLength(250);

                entity.Property(e => e.LidlorderHeaders)
                    .HasMaxLength(250)
                    .HasColumnName("LIDLOrderHeaders");

                entity.Property(e => e.LidlorderLines)
                    .HasMaxLength(250)
                    .HasColumnName("LIDLOrderLines");

                entity.Property(e => e.PalletCardsLidl)
                    .HasMaxLength(250)
                    .HasColumnName("PalletCardsLIDL");
            });

            modelBuilder.Entity<LoadOrder>(entity =>
            {
                entity.ToTable("LoadOrder");

                entity.Property(e => e.LoadOrderId).HasColumnName("LoadOrderID");

                entity.Property(e => e.LoadId).HasColumnName("LoadID");

                entity.Property(e => e.OrderId).HasColumnName("OrderID");

                entity.HasOne(d => d.Load)
                    .WithMany(p => p.LoadOrders)
                    .HasForeignKey(d => d.LoadId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadOrder_Load");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.LoadOrders)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadOrder_Order");
            });

            modelBuilder.Entity<LoadOrderFilter>(entity =>
            {
                entity.ToTable("LoadOrderFilter");

                entity.Property(e => e.LoadOrderFilterId).HasColumnName("LoadOrderFilterID");

                entity.Property(e => e.DocumentFrom).HasColumnType("datetime");

                entity.Property(e => e.DocumentUntil).HasColumnType("datetime");

                entity.Property(e => e.PurchaseFrom).HasColumnType("datetime");

                entity.Property(e => e.PurchaseUntil).HasColumnType("datetime");

                entity.Property(e => e.RequestedFrom).HasColumnType("datetime");

                entity.Property(e => e.RequestedUntil).HasColumnType("datetime");

                entity.Property(e => e.Text).HasMaxLength(100);

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.LoadOrderFilters)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadOrderFilter_User");
            });

            modelBuilder.Entity<LoadOrderPallet>(entity =>
            {
                entity.ToTable("LoadOrderPallet");

                entity.Property(e => e.LoadOrderPalletId)
                    .ValueGeneratedNever()
                    .HasColumnName("LoadOrderPalletID");

                entity.Property(e => e.LoadId).HasColumnName("LoadID");

                entity.Property(e => e.OrderId)
                    .ValueGeneratedOnAdd()
                    .HasColumnName("OrderID");

                entity.Property(e => e.PalletId).HasColumnName("PalletID");

                entity.HasOne(d => d.Load)
                    .WithMany(p => p.LoadOrderPallets)
                    .HasForeignKey(d => d.LoadId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadOrderPallet_Load");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.LoadOrderPallets)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadOrderPallet_Order1");

                entity.HasOne(d => d.Pallet)
                    .WithMany(p => p.LoadOrderPallets)
                    .HasForeignKey(d => d.PalletId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadOrderPallet_Pallet");
            });

            modelBuilder.Entity<LoadPallet>(entity =>
            {
                entity.ToTable("LoadPallet");

                entity.Property(e => e.LoadPalletId).HasColumnName("LoadPalletID");

                entity.Property(e => e.LoadId).HasColumnName("LoadID");

                entity.Property(e => e.PalletId).HasColumnName("PalletID");

                entity.HasOne(d => d.Load)
                    .WithMany(p => p.LoadPallets)
                    .HasForeignKey(d => d.LoadId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadPallet_Load");

                entity.HasOne(d => d.Pallet)
                    .WithMany(p => p.LoadPallets)
                    .HasForeignKey(d => d.PalletId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_LoadPallet_Pallet");
            });

            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("Material");

                entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

                entity.Property(e => e.Bun)
                    .HasMaxLength(10)
                    .HasColumnName("BUn");

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.Customer).HasMaxLength(20);

                entity.Property(e => e.CustomerMaterialNumber).HasMaxLength(20);

                entity.Property(e => e.Eanupc)
                    .HasMaxLength(50)
                    .HasColumnName("EANUPC");

                entity.Property(e => e.Gtin)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("GTIN");

                entity.Property(e => e.Language)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.MaterialDescription)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.MaterialSapid)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("MaterialSAPID");

                entity.Property(e => e.Name1).HasMaxLength(50);

                entity.Property(e => e.PackagingLineDescription)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductFamilyId).HasColumnName("ProductFamilyID");

                entity.Property(e => e.Seasonability).HasMaxLength(10);

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.Materials)
                    .HasForeignKey(d => d.CountryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Material_Country__");

                entity.HasOne(d => d.ProductFamily)
                    .WithMany(p => p.Materials)
                    .HasForeignKey(d => d.ProductFamilyId)
                    .HasConstraintName("FK_Material_ProductFamily__");
            });

            modelBuilder.Entity<Material1>(entity =>
            {
                entity.HasKey(e => e.MaterialId)
                    .HasName("PK_Material");

                entity.ToTable("Material_");

                entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

                entity.Property(e => e.Bun)
                    .HasMaxLength(10)
                    .HasColumnName("BUn");

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.Customer).HasMaxLength(20);

                entity.Property(e => e.CustomerMaterialNumber).HasMaxLength(20);

                entity.Property(e => e.Eanupc)
                    .HasMaxLength(50)
                    .HasColumnName("EANUPC");

                entity.Property(e => e.Gtin)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("GTIN");

                entity.Property(e => e.Language)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(e => e.MaterialDescription)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.MaterialSapid)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("MaterialSAPID");

                entity.Property(e => e.Name1).HasMaxLength(50);

                entity.Property(e => e.PackagingLineDescription)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductFamilyId).HasColumnName("ProductFamilyID");

                entity.Property(e => e.Seasonability).HasMaxLength(10);

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.Material1s)
                    .HasForeignKey(d => d.CountryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Material_Country");

                entity.HasOne(d => d.ProductFamily)
                    .WithMany(p => p.Material1s)
                    .HasForeignKey(d => d.ProductFamilyId)
                    .HasConstraintName("FK_Material__ProductFamily");
            });

            modelBuilder.Entity<MaterialItem>(entity =>
            {
                entity.HasNoKey();

                entity.Property(e => e.ItemDescription).HasMaxLength(250);

                entity.Property(e => e.ItemsNumber).HasColumnType("decimal(10, 4)");

                entity.Property(e => e.MaterialDescription).HasMaxLength(250);
            });

            modelBuilder.Entity<MaterialLagerdaysExpirationdaysTemp>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("MATERIAL_LAGERDAYS_EXPIRATIONDAYS_TEMP");

                entity.Property(e => e.MaterilId).HasColumnName("MaterilID");
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Order");

                entity.Property(e => e.OrderId).HasColumnName("OrderID");

                entity.Property(e => e.DocumentDate).HasColumnType("datetime");

                entity.Property(e => e.FileName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.InsertionDateTime).HasColumnType("datetime");

                entity.Property(e => e.LastModificationDateTime).HasColumnType("datetime");

                entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

                entity.Property(e => e.PurchaseOrderDate).HasColumnType("datetime");

                entity.Property(e => e.ReferenceNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.RequestedDeliveryDate).HasColumnType("datetime");

                entity.HasOne(d => d.Material)
                    .WithMany(p => p.Orders)
                    .HasForeignKey(d => d.MaterialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Order_Material_");
            });

            modelBuilder.Entity<PackagingLineForm>(entity =>
            {
                entity.ToTable("PackagingLineForm");

                entity.Property(e => e.PackagingLineFormId).HasColumnName("PackagingLineFormID");

                entity.Property(e => e.CompletionDateTime).HasColumnType("datetime");

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.FormCreationDate).HasColumnType("datetime");

                entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

                entity.Property(e => e.Notes).HasMaxLength(50);

                entity.Property(e => e.RegistrationDateTime).HasColumnType("datetime");

                entity.HasOne(d => d.Material)
                    .WithMany(p => p.PackagingLineForms)
                    .HasForeignKey(d => d.MaterialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PackagingLine_Material");
            });

            modelBuilder.Entity<PackagingLineFormPallet>(entity =>
            {
                entity.ToTable("PackagingLineFormPallet");

                entity.Property(e => e.PackagingLineFormPalletId).HasColumnName("PackagingLineFormPalletID");

                entity.Property(e => e.PackagingLineFormId).HasColumnName("PackagingLineFormID");

                entity.Property(e => e.PalletId).HasColumnName("PalletID");

                entity.HasOne(d => d.PackagingLineForm)
                    .WithMany(p => p.PackagingLineFormPallets)
                    .HasForeignKey(d => d.PackagingLineFormId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PackagingLineForm_PackagingLineFormPallet");

                entity.HasOne(d => d.Pallet)
                    .WithMany(p => p.PackagingLineFormPallets)
                    .HasForeignKey(d => d.PalletId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_PalletPackagingPalletTime_PackagingPalletTime");
            });

            modelBuilder.Entity<Pallet>(entity =>
            {
                entity.ToTable("Pallet");

                entity.Property(e => e.PalletId).HasColumnName("PalletID");

                entity.Property(e => e.BarCode)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.CountryId).HasColumnName("CountryID");

                entity.Property(e => e.MaterialId).HasColumnName("MaterialID");

                entity.Property(e => e.PalletCard)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PalletCardLidl)
                    .HasMaxLength(100)
                    .HasColumnName("PalletCardLIDL");

                entity.Property(e => e.PalletCreationDateTime).HasColumnType("datetime");

                entity.Property(e => e.Qrcode)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("QRCode");

                entity.Property(e => e.Sscc)
                    .HasMaxLength(50)
                    .HasColumnName("SSCC");

                entity.HasOne(d => d.Country)
                    .WithMany(p => p.Pallets)
                    .HasForeignKey(d => d.CountryId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Pallet_Country");

                entity.HasOne(d => d.Material)
                    .WithMany(p => p.Pallets)
                    .HasForeignKey(d => d.MaterialId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Pallet_Material");
            });

            modelBuilder.Entity<ProductFamily>(entity =>
            {
                entity.ToTable("ProductFamily");

                entity.Property(e => e.ProductFamilyId)
                    .ValueGeneratedNever()
                    .HasColumnName("ProductFamilyID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(20);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");

                entity.Property(e => e.RoleId)
                    .ValueGeneratedNever()
                    .HasColumnName("RoleID");

                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<SapA600>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A600");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapA601>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A601");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapA602>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A602");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapA603>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A603");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapA604>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A604");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapA605>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A605");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapA606>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_A606");

                entity.Property(e => e.Datbi)
                    .HasColumnType("datetime")
                    .HasColumnName("DATBI");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(255)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.KunnrV)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR_V");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");
            });

            modelBuilder.Entity<SapCustomerMaterialMasterKnmt>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_CustomerMaterialMaster_KNMT");

                entity.Property(e => e.BaseUnitOfMeasureMeins)
                    .HasMaxLength(255)
                    .HasColumnName("BaseUnitOfMeasure_MEINS");

                entity.Property(e => e.CustomerKunnr)
                    .HasMaxLength(250)
                    .HasColumnName("Customer_KUNNR");

                entity.Property(e => e.CustomerMaterialNumberKdmat)
                    .HasMaxLength(255)
                    .HasColumnName("CustomerMaterialNumber_KDMAT");

                entity.Property(e => e.MaterialMatnr).HasColumnName("Material_MATNR");
            });

            modelBuilder.Entity<SapDataperwarehouseMlgn>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_DATAPERWAREHOUSE_MLGN");

                entity.Property(e => e.KivotiaanapaletaLhmg1).HasColumnName("KIVOTIAANAPALETA_LHMG1");

                entity.Property(e => e.MaterialMatnr).HasColumnName("Material_MATNR");

                entity.Property(e => e.ModadametrisisLhme1)
                    .HasMaxLength(250)
                    .HasColumnName("MODADAMETRISIS_LHME1");

                entity.Property(e => e.WarehouseNumberLgnum)
                    .HasMaxLength(250)
                    .HasColumnName("WarehouseNumber_LGNUM");
            });

            modelBuilder.Entity<SapGeneralDataCustomerMaterKna1>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_GeneralDataCustomerMater_KNA1");

                entity.Property(e => e.Adrnr)
                    .HasMaxLength(250)
                    .HasColumnName("ADRNR");

                entity.Property(e => e.Bbbnr)
                    .HasMaxLength(250)
                    .HasColumnName("BBBNR");

                entity.Property(e => e.Bbsnr)
                    .HasMaxLength(250)
                    .HasColumnName("BBSNR");

                entity.Property(e => e.Bubkz)
                    .HasMaxLength(250)
                    .HasColumnName("BUBKZ");

                entity.Property(e => e.Kunnr)
                    .HasMaxLength(250)
                    .HasColumnName("KUNNR");

                entity.Property(e => e.Land1)
                    .HasMaxLength(250)
                    .HasColumnName("LAND1");

                entity.Property(e => e.Mcod1)
                    .HasMaxLength(250)
                    .HasColumnName("MCOD1");

                entity.Property(e => e.Mcod2)
                    .HasMaxLength(250)
                    .HasColumnName("MCOD2");

                entity.Property(e => e.Mcod3)
                    .HasMaxLength(250)
                    .HasColumnName("MCOD3");

                entity.Property(e => e.Name1)
                    .HasMaxLength(250)
                    .HasColumnName("NAME1");

                entity.Property(e => e.Name2)
                    .HasMaxLength(250)
                    .HasColumnName("NAME2");

                entity.Property(e => e.Ort01)
                    .HasMaxLength(250)
                    .HasColumnName("ORT01");

                entity.Property(e => e.Pstlz)
                    .HasMaxLength(250)
                    .HasColumnName("PSTLZ");

                entity.Property(e => e.Regio)
                    .HasMaxLength(250)
                    .HasColumnName("REGIO");

                entity.Property(e => e.Sortl)
                    .HasMaxLength(250)
                    .HasColumnName("SORTL");

                entity.Property(e => e.Spras)
                    .HasMaxLength(250)
                    .HasColumnName("SPRAS");

                entity.Property(e => e.Stcd1)
                    .HasMaxLength(250)
                    .HasColumnName("STCD1");

                entity.Property(e => e.Stcd2)
                    .HasMaxLength(250)
                    .HasColumnName("STCD2");

                entity.Property(e => e.Stceg)
                    .HasMaxLength(250)
                    .HasColumnName("STCEG");

                entity.Property(e => e.Stkzu)
                    .HasMaxLength(250)
                    .HasColumnName("STKZU");

                entity.Property(e => e.Stras)
                    .HasMaxLength(250)
                    .HasColumnName("STRAS");

                entity.Property(e => e.Telf1)
                    .HasMaxLength(250)
                    .HasColumnName("TELF1");

                entity.Property(e => e.Telfx)
                    .HasMaxLength(250)
                    .HasColumnName("TELFX");
            });

            modelBuilder.Entity<SapGeneralMaterialDataMara>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_GeneralMaterialData_MARA");

                entity.Property(e => e.EanupcEan11)
                    .HasMaxLength(255)
                    .HasColumnName("EANUPC_EAN11");

                entity.Property(e => e.MaterialFamilyExtwg)
                    .HasMaxLength(255)
                    .HasColumnName("MaterialFamily_EXTWG");

                entity.Property(e => e.MaterialMatnr).HasColumnName("Material_MATNR");

                entity.Property(e => e.SeasonalityGroes)
                    .HasMaxLength(250)
                    .HasColumnName("Seasonality_GROES");
            });

            modelBuilder.Entity<SapKonp>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_KONP");

                entity.Property(e => e.Bomat)
                    .HasMaxLength(255)
                    .HasColumnName("BOMAT");

                entity.Property(e => e.Kbetr)
                    .HasColumnType("decimal(30, 2)")
                    .HasColumnName("KBETR");

                entity.Property(e => e.Knumh)
                    .HasMaxLength(250)
                    .HasColumnName("KNUMH");

                entity.Property(e => e.Kunnr)
                    .HasMaxLength(255)
                    .HasColumnName("KUNNR");

                entity.Property(e => e.LoevmKo)
                    .HasMaxLength(255)
                    .HasColumnName("LOEVM_KO");
            });

            modelBuilder.Entity<SapMast>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_MAST");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");

                entity.Property(e => e.Stlal).HasColumnName("STLAL");

                entity.Property(e => e.Stlan)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("STLAN");

                entity.Property(e => e.Stlnr).HasColumnName("STLNR");

                entity.Property(e => e.Werks)
                    .HasMaxLength(200)
                    .IsUnicode(false)
                    .HasColumnName("WERKS");
            });

            modelBuilder.Entity<SapMaterialDescriptionMakt>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_MaterialDescription_MAKT");

                entity.Property(e => e.LanguageKeySpras)
                    .HasMaxLength(250)
                    .HasColumnName("LanguageKey_SPRAS");

                entity.Property(e => e.MaterialDescriptionMaktx)
                    .HasMaxLength(250)
                    .HasColumnName("MaterialDescription_MAKTX");

                entity.Property(e => e.MaterialNumberMatnr).HasColumnName("MaterialNumber_MATNR");
            });

            modelBuilder.Entity<SapStko>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_STKO");

                entity.Property(e => e.Bmeng)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("BMENG");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");

                entity.Property(e => e.Stlal).HasColumnName("STLAL");

                entity.Property(e => e.Stlnr).HasColumnName("STLNR");

                entity.Property(e => e.Stlty)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("STLTY");
            });

            modelBuilder.Entity<SapStpo>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_STPO");

                entity.Property(e => e.Idnrk).HasColumnName("IDNRK");

                entity.Property(e => e.Menge)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("MENGE");

                entity.Property(e => e.Stlnr).HasColumnName("STLNR");

                entity.Property(e => e.Stlty)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("STLTY");
            });

            modelBuilder.Entity<SapZgtinTable>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_ZGTIN_TABLE");

                entity.Property(e => e.CustomerNumberKunnr)
                    .HasMaxLength(250)
                    .HasColumnName("CustomerNumber_KUNNR");

                entity.Property(e => e.Gtin)
                    .HasMaxLength(250)
                    .HasColumnName("GTIN");

                entity.Property(e => e.Matnr).HasColumnName("MATNR");

                entity.Property(e => e.ValidTo)
                    .HasMaxLength(250)
                    .HasColumnName("VALID_TO");
            });

            modelBuilder.Entity<SapZmatinfo>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("SAP_ZMATINFO");

                entity.Property(e => e.Grweight)
                    .HasMaxLength(250)
                    .HasColumnName("GRWEIGHT");

                entity.Property(e => e.Height)
                    .HasMaxLength(250)
                    .HasColumnName("HEIGHT");

                entity.Property(e => e.MaterialMatnr).HasColumnName("Material_MATNR");

                entity.Property(e => e.Netweight)
                    .HasMaxLength(250)
                    .HasColumnName("NETWEIGHT");

                entity.Property(e => e.PiecesPerCartonPcspercar)
                    .HasMaxLength(250)
                    .HasColumnName("PiecesPerCarton_PCSPERCAR");
            });

            modelBuilder.Entity<Truck>(entity =>
            {
                entity.ToTable("Truck");

                entity.Property(e => e.TruckId).HasColumnName("TruckID");

                entity.Property(e => e.Cmrdocuments)
                    .HasMaxLength(100)
                    .HasColumnName("CMRDocuments");

                entity.Property(e => e.CompletionDateTime).HasColumnType("datetime");

                entity.Property(e => e.CreationDateTime).HasColumnType("datetime");

                entity.Property(e => e.DriverDocuments)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LidlordersHeaders)
                    .HasMaxLength(100)
                    .HasColumnName("LIDLOrdersHeaders");

                entity.Property(e => e.LidlordersLines)
                    .HasMaxLength(100)
                    .HasColumnName("LIDLOrdersLines");

                entity.Property(e => e.PalletCardLidl)
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnName("PalletCardLIDL");
            });

            modelBuilder.Entity<TruckOrderPallet>(entity =>
            {
                entity.ToTable("TruckOrderPallet");

                entity.Property(e => e.TruckOrderPalletId).HasColumnName("TruckOrderPalletID");

                entity.Property(e => e.OrderId).HasColumnName("OrderID");

                entity.Property(e => e.PalletId).HasColumnName("PalletID");

                entity.Property(e => e.TruckId).HasColumnName("TruckID");

                entity.HasOne(d => d.Order)
                    .WithMany(p => p.TruckOrderPallets)
                    .HasForeignKey(d => d.OrderId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TruckOrderPallet_Order");

                entity.HasOne(d => d.Pallet)
                    .WithMany(p => p.TruckOrderPallets)
                    .HasForeignKey(d => d.PalletId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TruckOrderPallet_Pallet");

                entity.HasOne(d => d.Truck)
                    .WithMany(p => p.TruckOrderPallets)
                    .HasForeignKey(d => d.TruckId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_TruckOrderPallet_Truck");
            });

            modelBuilder.Entity<UpdateFromSaplog>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("UpdateFromSAPLogs");

                entity.Property(e => e.Date).HasColumnType("datetime");

                entity.Property(e => e.Text).HasMaxLength(1000);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");

                entity.Property(e => e.UserId).HasColumnName("UserID");

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Firstname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.IdentityId)
                    .HasMaxLength(50)
                    .HasColumnName("IdentityID");

                entity.Property(e => e.LastConnectionDateTime).HasColumnType("datetime");

                entity.Property(e => e.Lastname)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.RegistrationDateTime).HasColumnType("datetime");

                entity.Property(e => e.RoleId).HasColumnName("RoleID");

                entity.Property(e => e.Telephone)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_User_Role");
            });

            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.ToTable("Warehouse");

                entity.Property(e => e.WarehouseId).HasColumnName("WarehouseID");

                entity.Property(e => e.LoadingAreaId).HasColumnName("LoadingAreaID");

                entity.Property(e => e.ModificationDateTime).HasColumnType("datetime");

                entity.Property(e => e.PalletId).HasColumnName("PalletID");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
