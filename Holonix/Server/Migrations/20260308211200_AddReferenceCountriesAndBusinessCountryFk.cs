using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holonix.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceCountriesAndBusinessCountryFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reference");

            migrationBuilder.CreateTable(
                name: "Country",
                schema: "reference",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TwoLetterIsoCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    ThreeLetterIsoCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.CountryId);
                });

            migrationBuilder.Sql(GetSeedCountriesSql());

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                schema: "business",
                table: "BusinessDetails",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE details
                SET details.[CountryId] = country.[CountryId]
                FROM [business].[BusinessDetails] AS details
                INNER JOIN [reference].[Country] AS country
                    ON UPPER(LTRIM(RTRIM(details.[Country]))) = UPPER(country.[Name])
                    OR UPPER(LTRIM(RTRIM(details.[Country]))) = UPPER(country.[TwoLetterIsoCode])
                    OR UPPER(LTRIM(RTRIM(details.[Country]))) = UPPER(country.[ThreeLetterIsoCode]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDetails_CountryId",
                schema: "business",
                table: "BusinessDetails",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Country_Name",
                schema: "reference",
                table: "Country",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Country_ThreeLetterIsoCode",
                schema: "reference",
                table: "Country",
                column: "ThreeLetterIsoCode",
                unique: true,
                filter: "[ThreeLetterIsoCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Country_TwoLetterIsoCode",
                schema: "reference",
                table: "Country",
                column: "TwoLetterIsoCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessDetails_Country_CountryId",
                schema: "business",
                table: "BusinessDetails",
                column: "CountryId",
                principalSchema: "reference",
                principalTable: "Country",
                principalColumn: "CountryId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "business",
                table: "BusinessDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "business",
                table: "BusinessDetails",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE details
                SET details.[Country] = country.[Name]
                FROM [business].[BusinessDetails] AS details
                INNER JOIN [reference].[Country] AS country
                    ON details.[CountryId] = country.[CountryId];
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessDetails_Country_CountryId",
                schema: "business",
                table: "BusinessDetails");

            migrationBuilder.DropTable(
                name: "Country",
                schema: "reference");

            migrationBuilder.DropIndex(
                name: "IX_BusinessDetails_CountryId",
                schema: "business",
                table: "BusinessDetails");

            migrationBuilder.DropColumn(
                name: "CountryId",
                schema: "business",
                table: "BusinessDetails");
        }

        private static string GetSeedCountriesSql()
        {
            return
                """
                INSERT INTO [reference].[Country] ([Name], [TwoLetterIsoCode], [ThreeLetterIsoCode])
                VALUES
                (N'Afghanistan', N'AF', N'AFG'),
                (N'Åland Islands', N'AX', N'ALA'),
                (N'Albania', N'AL', N'ALB'),
                (N'Algeria', N'DZ', N'DZA'),
                (N'American Samoa', N'AS', N'ASM'),
                (N'Andorra', N'AD', N'AND'),
                (N'Angola', N'AO', N'AGO'),
                (N'Anguilla', N'AI', N'AIA'),
                (N'Antigua & Barbuda', N'AG', N'ATG'),
                (N'Argentina', N'AR', N'ARG'),
                (N'Armenia', N'AM', N'ARM'),
                (N'Aruba', N'AW', N'ABW'),
                (N'Australia', N'AU', N'AUS'),
                (N'Austria', N'AT', N'AUT'),
                (N'Azerbaijan', N'AZ', N'AZE'),
                (N'Bahamas', N'BS', N'BHS'),
                (N'Bahrain', N'BH', N'BHR'),
                (N'Bangladesh', N'BD', N'BGD'),
                (N'Barbados', N'BB', N'BRB'),
                (N'Belarus', N'BY', N'BLR'),
                (N'Belgium', N'BE', N'BEL'),
                (N'Belize', N'BZ', N'BLZ'),
                (N'Benin', N'BJ', N'BEN'),
                (N'Bermuda', N'BM', N'BMU'),
                (N'Bhutan', N'BT', N'BTN'),
                (N'Bolivia', N'BO', N'BOL'),
                (N'Bonaire, Sint Eustatius and Saba', N'BQ', N'BES'),
                (N'Bosnia & Herzegovina', N'BA', N'BIH'),
                (N'Botswana', N'BW', N'BWA'),
                (N'Brazil', N'BR', N'BRA'),
                (N'British Indian Ocean Territory', N'IO', N'IOT'),
                (N'British Virgin Islands', N'VG', N'VGB'),
                (N'Brunei', N'BN', N'BRN'),
                (N'Bulgaria', N'BG', N'BGR'),
                (N'Burkina Faso', N'BF', N'BFA'),
                (N'Burundi', N'BI', N'BDI'),
                (N'Cabo Verde', N'CV', N'CPV'),
                (N'Cambodia', N'KH', N'KHM'),
                (N'Cameroon', N'CM', N'CMR'),
                (N'Canada', N'CA', N'CAN'),
                (N'Cayman Islands', N'KY', N'CYM'),
                (N'Central African Republic', N'CF', N'CAF'),
                (N'Chad', N'TD', N'TCD'),
                (N'Chile', N'CL', N'CHL'),
                (N'China', N'CN', N'CHN'),
                (N'Christmas Island', N'CX', N'CXR'),
                (N'Cocos (Keeling) Islands', N'CC', N'CCK'),
                (N'Colombia', N'CO', N'COL'),
                (N'Comoros', N'KM', N'COM'),
                (N'Republic of the Congo', N'CG', N'COG'),
                (N'Democratic Republic of the Congo', N'CD', N'COD'),
                (N'Cook Islands', N'CK', N'COK'),
                (N'Costa Rica', N'CR', N'CRI'),
                (N'Côte d’Ivoire', N'CI', N'CIV'),
                (N'Croatia', N'HR', N'HRV'),
                (N'Cuba', N'CU', N'CUB'),
                (N'Curaçao', N'CW', N'CUW'),
                (N'Cyprus', N'CY', N'CYP'),
                (N'Czechia', N'CZ', N'CZE'),
                (N'Denmark', N'DK', N'DNK'),
                (N'Djibouti', N'DJ', N'DJI'),
                (N'Dominica', N'DM', N'DMA'),
                (N'Dominican Republic', N'DO', N'DOM'),
                (N'Ecuador', N'EC', N'ECU'),
                (N'Egypt', N'EG', N'EGY'),
                (N'El Salvador', N'SV', N'SLV'),
                (N'Equatorial Guinea', N'GQ', N'GNQ'),
                (N'Eritrea', N'ER', N'ERI'),
                (N'Estonia', N'EE', N'EST'),
                (N'Eswatini', N'SZ', N'SWZ'),
                (N'Ethiopia', N'ET', N'ETH'),
                (N'Falkland Islands', N'FK', N'FLK'),
                (N'Faroe Islands', N'FO', N'FRO'),
                (N'Fiji', N'FJ', N'FJI'),
                (N'Finland', N'FI', N'FIN'),
                (N'France', N'FR', N'FRA'),
                (N'French Guiana', N'GF', N'GUF'),
                (N'French Polynesia', N'PF', N'PYF'),
                (N'Gabon', N'GA', N'GAB'),
                (N'Gambia', N'GM', N'GMB'),
                (N'Georgia', N'GE', N'GEO'),
                (N'Germany', N'DE', N'DEU'),
                (N'Ghana', N'GH', N'GHA'),
                (N'Gibraltar', N'GI', N'GIB'),
                (N'Greece', N'GR', N'GRC'),
                (N'Greenland', N'GL', N'GRL'),
                (N'Grenada', N'GD', N'GRD'),
                (N'Guadeloupe', N'GP', N'GLP'),
                (N'Guam', N'GU', N'GUM'),
                (N'Guatemala', N'GT', N'GTM'),
                (N'Guernsey', N'GG', N'GGY'),
                (N'Guinea', N'GN', N'GIN'),
                (N'Guinea-Bissau', N'GW', N'GNB'),
                (N'Guyana', N'GY', N'GUY'),
                (N'Haiti', N'HT', N'HTI'),
                (N'Honduras', N'HN', N'HND'),
                (N'Hong Kong', N'HK', N'HKG'),
                (N'Hungary', N'HU', N'HUN'),
                (N'Iceland', N'IS', N'ISL'),
                (N'India', N'IN', N'IND'),
                (N'Indonesia', N'ID', N'IDN'),
                (N'Iran', N'IR', N'IRN'),
                (N'Iraq', N'IQ', N'IRQ'),
                (N'Ireland', N'IE', N'IRL'),
                (N'Isle of Man', N'IM', N'IMN'),
                (N'Israel', N'IL', N'ISR'),
                (N'Italy', N'IT', N'ITA'),
                (N'Jamaica', N'JM', N'JAM'),
                (N'Japan', N'JP', N'JPN'),
                (N'Jersey', N'JE', N'JEY'),
                (N'Jordan', N'JO', N'JOR'),
                (N'Kazakhstan', N'KZ', N'KAZ'),
                (N'Kenya', N'KE', N'KEN'),
                (N'Kiribati', N'KI', N'KIR'),
                (N'South Korea', N'KR', N'KOR'),
                (N'Kosovo', N'XK', N'XKK'),
                (N'Kuwait', N'KW', N'KWT'),
                (N'Kyrgyzstan', N'KG', N'KGZ'),
                (N'Laos', N'LA', N'LAO'),
                (N'Latvia', N'LV', N'LVA'),
                (N'Lebanon', N'LB', N'LBN'),
                (N'Lesotho', N'LS', N'LSO'),
                (N'Liberia', N'LR', N'LBR'),
                (N'Libya', N'LY', N'LBY'),
                (N'Liechtenstein', N'LI', N'LIE'),
                (N'Lithuania', N'LT', N'LTU'),
                (N'Luxembourg', N'LU', N'LUX'),
                (N'Macau', N'MO', N'MAC'),
                (N'Madagascar', N'MG', N'MDG'),
                (N'Malawi', N'MW', N'MWI'),
                (N'Malaysia', N'MY', N'MYS'),
                (N'Maldives', N'MV', N'MDV'),
                (N'Mali', N'ML', N'MLI'),
                (N'Malta', N'MT', N'MLT'),
                (N'Marshall Islands', N'MH', N'MHL'),
                (N'Martinique', N'MQ', N'MTQ'),
                (N'Mauritania', N'MR', N'MRT'),
                (N'Mauritius', N'MU', N'MUS'),
                (N'Mayotte', N'YT', N'MYT'),
                (N'Mexico', N'MX', N'MEX'),
                (N'Micronesia', N'FM', N'FSM'),
                (N'Moldova', N'MD', N'MDA'),
                (N'Monaco', N'MC', N'MCO'),
                (N'Mongolia', N'MN', N'MNG'),
                (N'Montenegro', N'ME', N'MNE'),
                (N'Montserrat', N'MS', N'MSR'),
                (N'Morocco', N'MA', N'MAR'),
                (N'Mozambique', N'MZ', N'MOZ'),
                (N'Myanmar', N'MM', N'MMR'),
                (N'Namibia', N'NA', N'NAM'),
                (N'Nauru', N'NR', N'NRU'),
                (N'Nepal', N'NP', N'NPL'),
                (N'Netherlands', N'NL', N'NLD'),
                (N'New Caledonia', N'NC', N'NCL'),
                (N'New Zealand', N'NZ', N'NZL'),
                (N'Nicaragua', N'NI', N'NIC'),
                (N'Niger', N'NE', N'NER'),
                (N'Nigeria', N'NG', N'NGA'),
                (N'Niue', N'NU', N'NIU'),
                (N'Norfolk Island', N'NF', N'NFK'),
                (N'North Korea', N'KP', N'PRK'),
                (N'North Macedonia', N'MK', N'MKD'),
                (N'Northern Mariana Islands', N'MP', N'MNP'),
                (N'Norway', N'NO', N'NOR'),
                (N'Oman', N'OM', N'OMN'),
                (N'Pakistan', N'PK', N'PAK'),
                (N'Palau', N'PW', N'PLW'),
                (N'Palestine', N'PS', N'PSE'),
                (N'Panama', N'PA', N'PAN'),
                (N'Papua New Guinea', N'PG', N'PNG'),
                (N'Paraguay', N'PY', N'PRY'),
                (N'Peru', N'PE', N'PER'),
                (N'Philippines', N'PH', N'PHL'),
                (N'Pitcairn Islands', N'PN', N'PCN'),
                (N'Poland', N'PL', N'POL'),
                (N'Portugal', N'PT', N'PRT'),
                (N'Puerto Rico', N'PR', N'PRI'),
                (N'Qatar', N'QA', N'QAT'),
                (N'Reunion', N'RE', N'REU'),
                (N'Romania', N'RO', N'ROU'),
                (N'Russia', N'RU', N'RUS'),
                (N'Rwanda', N'RW', N'RWA'),
                (N'Samoa', N'WS', N'WSM'),
                (N'San Marino', N'SM', N'SMR'),
                (N'São Tomé & Príncipe', N'ST', N'STP'),
                (N'Saudi Arabia', N'SA', N'SAU'),
                (N'Senegal', N'SN', N'SEN'),
                (N'Serbia', N'RS', N'SRB'),
                (N'Seychelles', N'SC', N'SYC'),
                (N'Sierra Leone', N'SL', N'SLE'),
                (N'Singapore', N'SG', N'SGP'),
                (N'Sint Maarten', N'SX', N'SXM'),
                (N'Slovakia', N'SK', N'SVK'),
                (N'Slovenia', N'SI', N'SVN'),
                (N'Solomon Islands', N'SB', N'SLB'),
                (N'Somalia', N'SO', N'SOM'),
                (N'South Africa', N'ZA', N'ZAF'),
                (N'South Sudan', N'SS', N'SSD'),
                (N'Spain', N'ES', N'ESP'),
                (N'Sri Lanka', N'LK', N'LKA'),
                (N'St Helena, Ascension, Tristan da Cunha', N'SH', N'SHN'),
                (N'St. Barthélemy', N'BL', N'BLM'),
                (N'St. Kitts & Nevis', N'KN', N'KNA'),
                (N'St. Lucia', N'LC', N'LCA'),
                (N'St. Martin', N'MF', N'MAF'),
                (N'St. Pierre & Miquelon', N'PM', N'SPM'),
                (N'St. Vincent & Grenadines', N'VC', N'VCT'),
                (N'Sudan', N'SD', N'SDN'),
                (N'Suriname', N'SR', N'SUR'),
                (N'Svalbard & Jan Mayen', N'SJ', N'SJM'),
                (N'Sweden', N'SE', N'SWE'),
                (N'Switzerland', N'CH', N'CHE'),
                (N'Syria', N'SY', N'SYR'),
                (N'Taiwan', N'TW', N'TWN'),
                (N'Tajikistan', N'TJ', N'TJK'),
                (N'Tanzania', N'TZ', N'TZA'),
                (N'Thailand', N'TH', N'THA'),
                (N'Timor-Leste', N'TL', N'TLS'),
                (N'Togo', N'TG', N'TGO'),
                (N'Tokelau', N'TK', N'TKL'),
                (N'Tonga', N'TO', N'TON'),
                (N'Trinidad & Tobago', N'TT', N'TTO'),
                (N'Tunisia', N'TN', N'TUN'),
                (N'Turkey', N'TR', N'TUR'),
                (N'Turkmenistan', N'TM', N'TKM'),
                (N'Turks & Caicos Islands', N'TC', N'TCA'),
                (N'Tuvalu', N'TV', N'TUV'),
                (N'U.S. Outlying Islands', N'UM', N'UMI'),
                (N'U.S. Virgin Islands', N'VI', N'VIR'),
                (N'Uganda', N'UG', N'UGA'),
                (N'Ukraine', N'UA', N'UKR'),
                (N'United Arab Emirates', N'AE', N'ARE'),
                (N'United Kingdom', N'GB', N'GBR'),
                (N'United States', N'US', N'USA'),
                (N'Uruguay', N'UY', N'URY'),
                (N'Uzbekistan', N'UZ', N'UZB'),
                (N'Vanuatu', N'VU', N'VUT'),
                (N'Vatican City', N'VA', N'VAT'),
                (N'Venezuela', N'VE', N'VEN'),
                (N'Vietnam', N'VN', N'VNM'),
                (N'Wallis & Futuna', N'WF', N'WLF'),
                (N'Yemen', N'YE', N'YEM'),
                (N'Zambia', N'ZM', N'ZMB'),
                (N'Zimbabwe', N'ZW', N'ZWE');
                """;
        }
    }
}
