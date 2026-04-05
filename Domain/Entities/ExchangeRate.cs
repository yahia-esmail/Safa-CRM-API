namespace Domain.Entities;

public class ExchangeRate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }

    // USD is the base currency (always 1)
    public decimal USD { get; set; } = 1;

    // All other currencies: how many units = 1 USD
    public decimal AED { get; set; }   // UAE Dirham
    public decimal AFN { get; set; }   // Afghan Afghani
    public decimal ALL { get; set; }   // Albanian Lek
    public decimal AMD { get; set; }   // Armenian Dram
    public decimal ANG { get; set; }   // Netherlands Antillean Guilder
    public decimal AOA { get; set; }   // Angolan Kwanza
    public decimal ARS { get; set; }   // Argentine Peso
    public decimal AUD { get; set; }   // Australian Dollar
    public decimal AWG { get; set; }   // Aruban Florin
    public decimal AZN { get; set; }   // Azerbaijani Manat
    public decimal BAM { get; set; }   // Bosnia-Herzegovina Convertible Mark
    public decimal BBD { get; set; }   // Barbadian Dollar
    public decimal BDT { get; set; }   // Bangladeshi Taka
    public decimal BGN { get; set; }   // Bulgarian Lev
    public decimal BHD { get; set; }   // Bahraini Dinar
    public decimal BIF { get; set; }   // Burundian Franc
    public decimal BMD { get; set; }   // Bermudian Dollar
    public decimal BND { get; set; }   // Brunei Dollar
    public decimal BOB { get; set; }   // Bolivian Boliviano
    public decimal BRL { get; set; }   // Brazilian Real
    public decimal BSD { get; set; }   // Bahamian Dollar
    public decimal BTN { get; set; }   // Bhutanese Ngultrum
    public decimal BWP { get; set; }   // Botswana Pula
    public decimal BYN { get; set; }   // Belarusian Ruble
    public decimal BZD { get; set; }   // Belize Dollar
    public decimal CAD { get; set; }   // Canadian Dollar
    public decimal CDF { get; set; }   // Congolese Franc
    public decimal CHF { get; set; }   // Swiss Franc
    public decimal CLF { get; set; }   // Chilean Unit of Account (UF)
    public decimal CLP { get; set; }   // Chilean Peso
    public decimal CNH { get; set; }   // Chinese Yuan (Offshore)
    public decimal CNY { get; set; }   // Chinese Yuan
    public decimal COP { get; set; }   // Colombian Peso
    public decimal CRC { get; set; }   // Costa Rican Colón
    public decimal CUP { get; set; }   // Cuban Peso
    public decimal CVE { get; set; }   // Cape Verdean Escudo
    public decimal CZK { get; set; }   // Czech Koruna
    public decimal DJF { get; set; }   // Djiboutian Franc
    public decimal DKK { get; set; }   // Danish Krone
    public decimal DOP { get; set; }   // Dominican Peso
    public decimal DZD { get; set; }   // Algerian Dinar
    public decimal EGP { get; set; }   // Egyptian Pound
    public decimal ERN { get; set; }   // Eritrean Nakfa
    public decimal ETB { get; set; }   // Ethiopian Birr
    public decimal EUR { get; set; }   // Euro
    public decimal FJD { get; set; }   // Fijian Dollar
    public decimal FKP { get; set; }   // Falkland Islands Pound
    public decimal FOK { get; set; }   // Faroese Króna
    public decimal GBP { get; set; }   // British Pound Sterling
    public decimal GEL { get; set; }   // Georgian Lari
    public decimal GGP { get; set; }   // Guernsey Pound
    public decimal GHS { get; set; }   // Ghanaian Cedi
    public decimal GIP { get; set; }   // Gibraltar Pound
    public decimal GMD { get; set; }   // Gambian Dalasi
    public decimal GNF { get; set; }   // Guinean Franc
    public decimal GTQ { get; set; }   // Guatemalan Quetzal
    public decimal GYD { get; set; }   // Guyanese Dollar
    public decimal HKD { get; set; }   // Hong Kong Dollar
    public decimal HNL { get; set; }   // Honduran Lempira
    public decimal HRK { get; set; }   // Croatian Kuna
    public decimal HTG { get; set; }   // Haitian Gourde
    public decimal HUF { get; set; }   // Hungarian Forint
    public decimal IDR { get; set; }   // Indonesian Rupiah
    public decimal ILS { get; set; }   // Israeli New Shekel
    public decimal IMP { get; set; }   // Isle of Man Pound
    public decimal INR { get; set; }   // Indian Rupee
    public decimal IQD { get; set; }   // Iraqi Dinar
    public decimal IRR { get; set; }   // Iranian Rial
    public decimal ISK { get; set; }   // Icelandic Króna
    public decimal JEP { get; set; }   // Jersey Pound
    public decimal JMD { get; set; }   // Jamaican Dollar
    public decimal JOD { get; set; }   // Jordanian Dinar
    public decimal JPY { get; set; }   // Japanese Yen
    public decimal KES { get; set; }   // Kenyan Shilling
    public decimal KGS { get; set; }   // Kyrgyzstani Som
    public decimal KHR { get; set; }   // Cambodian Riel
    public decimal KID { get; set; }   // Kiribati Dollar
    public decimal KMF { get; set; }   // Comorian Franc
    public decimal KRW { get; set; }   // South Korean Won
    public decimal KWD { get; set; }   // Kuwaiti Dinar
    public decimal KYD { get; set; }   // Cayman Islands Dollar
    public decimal KZT { get; set; }   // Kazakhstani Tenge
    public decimal LAK { get; set; }   // Lao Kip
    public decimal LBP { get; set; }   // Lebanese Pound
    public decimal LKR { get; set; }   // Sri Lankan Rupee
    public decimal LRD { get; set; }   // Liberian Dollar
    public decimal LSL { get; set; }   // Lesotho Loti
    public decimal LYD { get; set; }   // Libyan Dinar
    public decimal MAD { get; set; }   // Moroccan Dirham
    public decimal MDL { get; set; }   // Moldovan Leu
    public decimal MGA { get; set; }   // Malagasy Ariary
    public decimal MKD { get; set; }   // Macedonian Denar
    public decimal MMK { get; set; }   // Myanmar Kyat
    public decimal MNT { get; set; }   // Mongolian Tögrög
    public decimal MOP { get; set; }   // Macanese Pataca
    public decimal MRU { get; set; }   // Mauritanian Ouguiya
    public decimal MUR { get; set; }   // Mauritian Rupee
    public decimal MVR { get; set; }   // Maldivian Rufiyaa
    public decimal MWK { get; set; }   // Malawian Kwacha
    public decimal MXN { get; set; }   // Mexican Peso
    public decimal MYR { get; set; }   // Malaysian Ringgit
    public decimal MZN { get; set; }   // Mozambican Metical
    public decimal NAD { get; set; }   // Namibian Dollar
    public decimal NGN { get; set; }   // Nigerian Naira
    public decimal NIO { get; set; }   // Nicaraguan Córdoba
    public decimal NOK { get; set; }   // Norwegian Krone
    public decimal NPR { get; set; }   // Nepalese Rupee
    public decimal NZD { get; set; }   // New Zealand Dollar
    public decimal OMR { get; set; }   // Omani Rial
    public decimal PAB { get; set; }   // Panamanian Balboa
    public decimal PEN { get; set; }   // Peruvian Sol
    public decimal PGK { get; set; }   // Papua New Guinean Kina
    public decimal PHP { get; set; }   // Philippine Peso
    public decimal PKR { get; set; }   // Pakistani Rupee
    public decimal PLN { get; set; }   // Polish Złoty
    public decimal PYG { get; set; }   // Paraguayan Guaraní
    public decimal QAR { get; set; }   // Qatari Riyal
    public decimal RON { get; set; }   // Romanian Leu
    public decimal RSD { get; set; }   // Serbian Dinar
    public decimal RUB { get; set; }   // Russian Ruble
    public decimal RWF { get; set; }   // Rwandan Franc
    public decimal SAR { get; set; }   // Saudi Riyal
    public decimal SBD { get; set; }   // Solomon Islands Dollar
    public decimal SCR { get; set; }   // Seychellois Rupee
    public decimal SDG { get; set; }   // Sudanese Pound
    public decimal SEK { get; set; }   // Swedish Krona
    public decimal SGD { get; set; }   // Singapore Dollar
    public decimal SHP { get; set; }   // Saint Helena Pound
    public decimal SLE { get; set; }   // Sierra Leonean Leone (new)
    public decimal SLL { get; set; }   // Sierra Leonean Leone (old)
    public decimal SOS { get; set; }   // Somali Shilling
    public decimal SRD { get; set; }   // Surinamese Dollar
    public decimal SSP { get; set; }   // South Sudanese Pound
    public decimal STN { get; set; }   // São Tomé and Príncipe Dobra
    public decimal SYP { get; set; }   // Syrian Pound
    public decimal SZL { get; set; }   // Eswatini Lilangeni
    public decimal THB { get; set; }   // Thai Baht
    public decimal TJS { get; set; }   // Tajikistani Somoni
    public decimal TMT { get; set; }   // Turkmenistani Manat
    public decimal TND { get; set; }   // Tunisian Dinar
    public decimal TOP { get; set; }   // Tongan Paʻanga
    public decimal TRY { get; set; }   // Turkish Lira
    public decimal TTD { get; set; }   // Trinidad and Tobago Dollar
    public decimal TVD { get; set; }   // Tuvaluan Dollar
    public decimal TWD { get; set; }   // New Taiwan Dollar
    public decimal TZS { get; set; }   // Tanzanian Shilling
    public decimal UAH { get; set; }   // Ukrainian Hryvnia
    public decimal UGX { get; set; }   // Ugandan Shilling
    public decimal UYU { get; set; }   // Uruguayan Peso
    public decimal UZS { get; set; }   // Uzbekistani Soʻm
    public decimal VES { get; set; }   // Venezuelan Bolívar Soberano
    public decimal VND { get; set; }   // Vietnamese Đồng
    public decimal VUV { get; set; }   // Vanuatu Vatu
    public decimal WST { get; set; }   // Samoan Tālā
    public decimal XAF { get; set; }   // Central African CFA Franc
    public decimal XCD { get; set; }   // East Caribbean Dollar
    public decimal XCG { get; set; }   // Caribbean Guilder
    public decimal XDR { get; set; }   // Special Drawing Rights
    public decimal XOF { get; set; }   // West African CFA Franc
    public decimal XPF { get; set; }   // CFP Franc
    public decimal YER { get; set; }   // Yemeni Rial
    public decimal ZAR { get; set; }   // South African Rand
    public decimal ZMW { get; set; }   // Zambian Kwacha
    public decimal ZWG { get; set; }   // Zimbabwe Gold
    public decimal ZWL { get; set; }   // Zimbabwean Dollar
}
