using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;
using AuctionSystem.Domain.ValueObjects;
using AuctionSystem.Infrastructure.Persistence;
using AuctionSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AuctionSystem.API.Data;

public static class DatabaseSeeder
{
    private const int TargetUserCount = 100;
    private const int TargetAuctionCount = 100;
    private const int TargetTransactionCount = 100;

    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (environment is null) throw new ArgumentNullException(nameof(environment));
        if (logger is null) throw new ArgumentNullException(nameof(logger));

        var seedingEnabled = configuration.GetValue<bool?>("DatabaseSeeding:Enabled") ?? environment.IsDevelopment();
        var resetDatabaseOnStartup = seedingEnabled && (configuration.GetValue<bool?>("DatabaseSeeding:ResetDatabaseOnStartup") ?? false);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var providerName = db.Database.ProviderName ?? string.Empty;

        if (resetDatabaseOnStartup)
        {
            logger.LogWarning("Database reset requested. Deleting and recreating the database before seeding.");

            if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
            else if (db.Database.IsRelational())
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.MigrateAsync(cancellationToken);
            }
            else
            {
                await db.Database.EnsureDeletedAsync(cancellationToken);
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        else if (providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }
        else if (db.Database.IsRelational())
        {
            try
            {
                await db.Database.MigrateAsync(cancellationToken);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Relational-specific methods", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.EnsureCreatedAsync(cancellationToken);
            }
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!seedingEnabled)
        {
            logger.LogInformation("Database schema is ready. Sample data seeding is disabled.");
            return;
        }

        if (!resetDatabaseOnStartup && await db.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains users. Skipping seed.");
            return;
        }

        var now = DateTime.UtcNow;
        var rng = new Random(21_022_026);

        var admin = User.Register("admin@auctions.local", "System Admin", UserRole.Admin, "+1 (555) 100-0001", now.AddDays(-365));
        var adminOps = User.Register("ops.admin@auctions.local", "Operations Admin", UserRole.Admin, "+1 (555) 100-0002", now.AddDays(-320));

        var sellerA = User.Register("seller.alpha@auctions.local", "Seller Alpha", UserRole.Seller, "+1 (555) 200-0001", now.AddDays(-280));
        var sellerB = User.Register("seller.bravo@auctions.local", "Seller Bravo", UserRole.Seller, "+1 (555) 200-0002", now.AddDays(-240));
        var sellerC = User.Register("seller.charlie@auctions.local", "Seller Charlie", UserRole.Seller, "+1 (555) 200-0003", now.AddDays(-200));

        var bidderA = User.Register("bidder.one@auctions.local", "Bidder One", UserRole.Bidder, "+1 (555) 300-0001", now.AddDays(-180));
        var bidderB = User.Register("bidder.two@auctions.local", "Bidder Two", UserRole.Bidder, "+1 (555) 300-0002", now.AddDays(-160));
        var bidderC = User.Register("bidder.three@auctions.local", "Bidder Three", UserRole.Bidder, "+1 (555) 300-0003", now.AddDays(-140));
        var bidderD = User.Register("bidder.four@auctions.local", "Bidder Four", UserRole.Bidder, "+1 (555) 300-0004", now.AddDays(-120));

        var inactiveBidder = User.Register("inactive.bidder@auctions.local", "Inactive Bidder", UserRole.Bidder, "+1 (555) 300-0099", now.AddDays(-90));
        inactiveBidder.Deactivate(now.AddDays(-30));

        var users = new List<User>
        {
            admin, adminOps, sellerA, sellerB, sellerC, bidderA, bidderB, bidderC, bidderD, inactiveBidder
        };

        while (users.Count < TargetUserCount)
        {
            var index = users.Count - 9;
            var role = index % 4 == 0 ? UserRole.Seller : UserRole.Bidder;
            var rolePrefix = role == UserRole.Seller ? "seller.bulk" : "bidder.bulk";
            var fullName = role == UserRole.Seller ? $"Seller Bulk {index:000}" : $"Bidder Bulk {index:000}";
            var phonePrefix = role == UserRole.Seller ? "400" : "500";

            users.Add(User.Register(
                $"{rolePrefix}{index:000}@auctions.local",
                fullName,
                role,
                $"+1 (555) {phonePrefix}-{index:0000}",
                now.AddDays(-(40 + index))));
        }

        db.Users.AddRange(users);

        var auctions = new List<Auction>();
        var sellers = users.Where(x => x.Role == UserRole.Seller && x.IsActive).ToArray();
        var bidders = users.Where(x => x.Role == UserRole.Bidder && x.IsActive).ToArray();
        var auctionSeedDefinitions = GetAuctionSeedDefinitions();

        for (var index = 0; index < auctionSeedDefinitions.Count; index++)
        {
            var definition = auctionSeedDefinitions[index];
            var seller = sellers[index % sellers.Length];
            var createdAt = now.AddDays(-(120 - (index % 90)));
            var endTime = now.AddDays(14 + (index % 45));

            var auction = CreateAuction(
                seller.Id,
                definition.Title,
                definition.Category,
                definition.Description,
                definition.StartingPriceAmount,
                endTime,
                createdAt);

            var startTime = createdAt.AddHours(2);
            auction.Start(startTime);

            var bidder = bidders[(index + 1) % bidders.Length];
            if (bidder.Id == seller.Id)
            {
                bidder = bidders[(index + 2) % bidders.Length];
            }

            var increment = 15m + rng.Next(10, 90);
            var amount = Money.Create(auction.CurrentPrice.Amount + increment, "USD");
            var bidPlacedAt = startTime.AddHours(2);
            auction.PlaceBid(bidder.Id, amount, bidPlacedAt);

            auctions.Add(auction);
        }

        db.Auctions.AddRange(auctions);

        var paymentMethods = users
            .Where(x => x.IsActive && x.Role != UserRole.Admin)
            .Select((user, idx) =>
            {
                var last4 = (1000 + (idx % 9000)).ToString();
                var provider = idx % 3 == 0 ? "Visa" : idx % 3 == 1 ? "Mastercard" : "Amex";
                return new PaymentMethod(
                    Guid.NewGuid(),
                    user.Id,
                    "Card",
                    provider,
                    last4,
                    (idx % 12) + 1,
                    now.Year + 2 + (idx % 4),
                    user.FullName,
                    true,
                    now.AddDays(-(60 + idx)));
            })
            .ToArray();
        db.PaymentMethods.AddRange(paymentMethods);

        var userPasswords = users
            .Select(u =>
            {
                var hash = PasswordHashing.Hash("Pass123!");
                return new UserPassword(u.Id, hash.Hash, hash.Salt, hash.Iterations, now.AddDays(-200));
            })
            .ToArray();
        db.UserPasswords.AddRange(userPasswords);

        var refunds = new List<AdminTransactionRefund>();
        db.AdminTransactionRefunds.AddRange(refunds);

        var settings = new[]
        {
            AdminSystemSetting.Create("maintenance.mode", "false", now.AddMinutes(-30), admin.Id),
            AdminSystemSetting.Create("auction.allowGuestBidding", "false", now.AddMinutes(-30), admin.Id),
            AdminSystemSetting.Create("auction.minBidIncrement", "5", now.AddMinutes(-30), admin.Id),
            AdminSystemSetting.Create("auction.extensionMinutes", "2", now.AddMinutes(-30), adminOps.Id),
            AdminSystemSetting.Create("payments.settlementWindowHours", "48", now.AddMinutes(-30), adminOps.Id)
        };
        db.AdminSystemSettings.AddRange(settings);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Database seeded successfully. Users: {UserCount}, Auctions: {AuctionCount}, Bids: {BidCount}, Refunds: {RefundCount}.",
            users.Count,
            auctions.Count,
            auctions.Sum(a => a.Bids.Count),
            refunds.Count);
    }

    private static Auction CreateAuction(
        Guid sellerId,
        string title,
        string category,
        string description,
        decimal startingPriceAmount,
        DateTime endTimeUtc,
        DateTime createdAtUtc)
    {
        return Auction.Create(
            sellerId,
            title,
            Money.Create(startingPriceAmount, "USD"),
            endTimeUtc,
            description,
            category,
            nowUtc: createdAtUtc);
    }

    private static IReadOnlyList<AuctionSeedDefinition> GetAuctionSeedDefinitions()
    {
        var promptDefinitions = new (string Title, string Description)[]
        {
            ("Vintage Omega Speedmaster wristwatch", "Stainless steel chronograph with black tachymeter bezel and aged tritium dial."),
            ("Rolex Datejust wristwatch", "Fluted bezel with jubilee bracelet and champagne dial."),
            ("Cartier Tank wristwatch", "Rectangular gold case with white Roman numeral dial and black leather strap."),
            ("Audemars Piguet Royal Oak wristwatch", "Octagonal bezel in brushed steel with deep blue dial."),
            ("Patek Philippe Calatrava wristwatch", "Minimalist round yellow gold case with ivory dial."),
            ("Jaeger-LeCoultre Reverso wristwatch", "Reversible art deco case with silver guilloche dial."),
            ("Tudor Black Bay wristwatch", "Burgundy bezel diver watch with rivet bracelet."),
            ("TAG Heuer Monaco wristwatch", "Square racing chronograph with blue sunburst dial."),
            ("Breitling Navitimer wristwatch", "Aviation slide-rule bezel with black dial and brown leather strap."),
            ("Grand Seiko Snowflake wristwatch", "Titanium case with textured white dial and spring drive display."),
            ("Leica M6 film camera", "Black rangefinder body with attached 35mm lens."),
            ("Hasselblad 500C medium format camera", "Chrome waist-level finder and Zeiss lens."),
            ("Nikon F2 film camera", "Black professional SLR with prism finder and prime lens."),
            ("Canon AE-1 film camera", "Silver-and-black body with classic 50mm lens."),
            ("Sony A7R V camera", "Modern mirrorless body with mounted G Master portrait lens."),
            ("Fujifilm X100V camera", "Compact silver street photography camera with fixed lens."),
            ("Polaroid SX-70 camera", "Folding instant camera in brown leather and brushed metal."),
            ("Rolleiflex twin-lens reflex camera", "Black vintage body with dual front lenses."),
            ("Pentax 67 camera", "Large medium format body with wood side grip and lens attached."),
            ("Contax G2 camera", "Titanium rangefinder-style body with Zeiss lens."),
            ("Framed Michael Jordan signed Chicago Bulls jersey", "Red championship-era jersey in black shadowbox frame."),
            ("Framed Kobe Bryant signed Lakers jersey", "Gold number 24 jersey with premium matting."),
            ("Framed striped football shirt display", "Sky blue and white striped shirt presented in a museum-style shadowbox frame."),
            ("Framed deep red football shirt display", "Deep red shirt presented in a clean black shadowbox frame."),
            ("Framed navy textile display", "Folded navy fabric garment with silver trim presented in a premium shadowbox frame."),
            ("Framed white textile display", "Folded white fabric garment with blue and gold accents presented in a premium shadowbox frame."),
            ("Framed sky blue textile display", "Sky blue fabric garment presented in a classic shadowbox frame."),
            ("Framed retro sweater display", "Retro knit sweater presented in a premium shadowbox frame."),
            ("Framed white textile top display", "Elegant white fabric top presented in a luxury shadowbox frame."),
            ("Framed yellow textile display", "Yellow fabric garment presented in an archival shadowbox frame."),
            ("Custom liquid-cooled gaming PC tower", "Black aluminum case with cyan interior glow and tempered glass side panel."),
            ("White RGB gaming PC tower", "Panoramic glass case with vertical GPU and clean cable management."),
            ("Small form factor gaming PC tower", "Matte charcoal cube case with subtle amber internal lighting."),
            ("Limited-edition gaming console", "Matte black body with copper accents and angular design."),
            ("Retro-inspired gaming console", "Compact cream body with modern ventilation and sculpted edges."),
            ("Premium handheld gaming console", "Black portable unit with edge-lit controls and glossy screen bezel."),
            ("Collector arcade stick controller", "Aluminum top plate with translucent buttons and premium joystick."),
            ("Racing simulator steering wheel base", "Carbon fiber wheel rim mounted to premium hub."),
            ("Limited-edition VR headset", "Sleek black visor with metallic trim and integrated strap."),
            ("High-end graphics card", "Triple-fan shroud in dark titanium finish with subtle RGB edge light."),
            ("Graded fantasy fire dragon trading card", "Iconic dragon-themed collectible card inside a premium clear slab."),
            ("Graded rare lotus fantasy trading card", "Ultra-rare botanical-themed collectible card sealed in archival slab."),
            ("Graded rookie baseball card", "Vintage 1950s player card in crystal-clear protective slab."),
            ("Graded basketball rookie trading card", "Classic basketball collectible card in premium slab holder."),
            ("Sealed fantasy monster trading card booster box", "Vintage foil booster box with intact factory wrap."),
            ("Sealed fantasy strategy trading card booster box", "Early-edition fantasy card box with crisp wrap."),
            ("Slabbed white dragon fantasy trading card", "High-grade dragon-themed collectible card in premium case."),
            ("Rare sports ticket stub in acrylic case", "Championship game collectible encased as a single display item."),
            ("Vintage baseball card in magnetic one-touch holder", "Classic player card presented as a single collectible."),
            ("Limited collector card binder", "Closed premium leather binder with embossed emblem for trading cards."),
            ("Framed abstract expressionist artwork", "Bold red and black gestural canvas in slim gallery frame."),
            ("Framed impressionist landscape artwork", "Luminous countryside painting in antique gold frame."),
            ("Framed monochrome street photography artwork", "Black-and-white city print in museum frame."),
            ("Framed pop art portrait artwork", "High-contrast neon portrait in modern acrylic frame."),
            ("Framed Japanese woodblock artwork", "Elegant ukiyo-e style print in natural wood frame."),
            ("Framed art deco poster artwork", "Geometric luxury travel poster in brushed brass frame."),
            ("Framed contemporary minimal artwork", "Oversized beige and charcoal composition in floating frame."),
            ("Framed surrealist artwork", "Dreamlike desert scene in dark walnut gallery frame."),
            ("Framed botanical illustration artwork", "Detailed scientific floral print in archival mat and frame."),
            ("Framed blueprint-style automotive artwork", "Technical supercar drawing in matte black frame."),
            ("Custom mechanical keyboard", "Navy aluminum case with brass weight and cream keycaps."),
            ("Split ergonomic mechanical keyboard", "Matte black dual-body layout with sculpted caps."),
            ("75 percent enthusiast keyboard", "Silver CNC case with dark green keycap set."),
            ("Retro beige mechanical keyboard", "Vintage terminal-inspired layout with thick dye-sub caps."),
            ("Transparent acrylic keyboard", "Layered clear chassis with visible internal components."),
            ("Compact 60 percent keyboard", "Burgundy anodized case with artisan escape key."),
            ("Low-profile wireless keyboard", "Ultra-thin dark graphite body with premium switches."),
            ("Industrial metal keyboard", "Gunmetal chassis with orange accent keys and exposed screws."),
            ("Luxury keyboard with marble wrist rest attached", "White aluminum body and soft gray keycaps."),
            ("Artisan-themed mechanical keyboard", "Forest green case with handcrafted resin novelty keys."),
            ("Open-back audiophile headphones", "Premium over-ear headset with brushed metal yokes."),
            ("Planar magnetic audiophile headphones", "Large circular earcups with premium leather headband."),
            ("Luxury wireless headphones", "Champagne aluminum earcups with tan leather padding."),
            ("Gaming headset with suspended headband", "Matte black frame and subtle cyan accent lighting."),
            ("Studio monitoring headphones", "Closed-back headset in satin black finish."),
            ("Retro hi-fi headphones", "Silver mesh earcups with thick cream headband cushion."),
            ("Premium DJ headphones", "Foldable over-ear design with glossy piano-black shells."),
            ("Limited-edition luxury headphones", "Deep oxblood leather ear cushions with gold trim."),
            ("Carbon fiber audiophile headphones", "Angular earcup shells with dark metallic frame."),
            ("Minimalist Scandinavian headphones", "Soft gray textile headband with clean matte earcups."),
            ("Iconic red white and black high-top sneaker", "Bold color-blocked single shoe shown as a single product."),
            ("Knit lifestyle sneaker", "Muted sand-toned single shoe with ribbed sole."),
            ("Luxury monogram-panel sneaker", "White leather single shoe with gray patterned side paneling."),
            ("Deconstructed designer sneaker", "Cream single shoe with industrial lace details."),
            ("Premium suede runner", "Made-in-USA-inspired single shoe in navy and gray."),
            ("Retro basketball sneaker", "Glossy patent leather single shoe in championship purple and gold."),
            ("Luxury designer low-top sneaker", "All-white single shoe with thick sculpted sole."),
            ("Vintage running sneaker", "Faded silver mesh single shoe with cream midsole."),
            ("Futuristic concept sneaker", "Black single shoe with aerodynamic shell and blue accents."),
            ("Skater sneaker collectible", "Worn-in black canvas single shoe with premium foxing detail."),
            ("Art deco desk lamp", "Restored brass base with green banker glass shade."),
            ("Stained glass floral table lamp", "Colorful floral shade with bronze stem."),
            ("Mid-century mushroom lamp", "Glossy orange dome shade with chrome stem."),
            ("Industrial workshop lamp", "Black enamel shade with articulated steel arm and weighted base."),
            ("Murano glass table lamp", "Swirled amber glass body with elegant tapered shade."),
            ("Ceramic artisan lamp", "Hand-glazed navy body with pleated linen shade."),
            ("Victorian oil-lamp conversion", "Ornate brass reservoir base with frosted chimney shade."),
            ("Scandinavian wood lamp", "Pale oak tripod base with simple white drum shade."),
            ("Brutalist sculptural lamp", "Dark textured metal body with geometric silhouette."),
            ("Luxury marble table lamp", "Black marble column base with slim satin brass stem and cream shade.")
        };

        return promptDefinitions
            .Select((definition, index) => new AuctionSeedDefinition(
                definition.Title,
                GetAuctionSeedCategory(index),
                definition.Description,
                GetAuctionSeedStartingPrice(index)))
            .ToArray();
    }

    private static string GetAuctionSeedCategory(int index)
    {
        if (index < 10) return "Luxury";
        if (index < 20) return "Tech";
        if (index < 30) return "Sports";
        if (index < 40) return "Gaming";
        if (index < 50) return "Collectibles";
        if (index < 60) return "Art";
        if (index < 70) return "Tech";
        if (index < 80) return "Audio";
        if (index < 90) return "Fashion";
        return "Home";
    }

    private static decimal GetAuctionSeedStartingPrice(int index)
    {
        var prices = new decimal[]
        {
            1200m, 2800m, 3400m, 5200m, 4800m, 3100m, 1850m, 2600m, 3950m, 4300m,
            900m, 1600m, 850m, 420m, 2500m, 1400m, 380m, 1250m, 1800m, 1350m,
            1400m, 1100m, 1800m, 1700m, 900m, 950m, 1600m, 1300m, 850m, 2200m,
            2400m, 2100m, 1750m, 650m, 420m, 500m, 320m, 780m, 950m, 700m,
            900m, 5000m, 320m, 2500m, 650m, 780m, 420m, 240m, 210m, 180m,
            1200m, 900m, 600m, 750m, 680m, 520m, 850m, 1100m, 430m, 560m,
            280m, 340m, 390m, 220m, 260m, 300m, 240m, 360m, 450m, 410m,
            950m, 1200m, 700m, 240m, 280m, 320m, 360m, 820m, 760m, 540m,
            600m, 480m, 950m, 720m, 380m, 410m, 680m, 290m, 520m, 260m,
            220m, 260m, 180m, 160m, 340m, 210m, 190m, 170m, 280m, 360m
        };

        return prices[index];
    }

    private sealed record AuctionSeedDefinition(
        string Title,
        string Category,
        string Description,
        decimal StartingPriceAmount);
}
