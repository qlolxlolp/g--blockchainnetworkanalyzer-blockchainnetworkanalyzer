"""
Iranian geography data - 31 provinces with cities and coordinates.
Complete geographic coverage for the Iranian Network Miner Detection System.
"""

from dataclasses import dataclass
from typing import List, Dict, Optional

@dataclass
class City:
    """Represents a city with its coordinates."""
    name: str
    latitude: float
    longitude: float


@dataclass
class Province:
    """Represents an Iranian province with cities."""
    name: str
    name_persian: str
    code: int
    latitude: float
    longitude: float
    cities: List[City]

    def get_city(self, name: str) -> Optional[City]:
        """Get city by name (case-insensitive)."""
        for city in self.cities:
            if city.name.lower() == name.lower():
                return city
        return None


# Complete Iranian provinces data (31 provinces)
IRANIAN_PROVINCES = [
    Province(
        name="Tehran",
        name_persian="تهران",
        code=1,
        latitude=35.6892,
        longitude=51.3890,
        cities=[
            City("Tehran", 35.6892, 51.3890),
            City("Rey", 35.6030, 51.4278),
            City("Shemiranat", 35.8036, 51.4142),
            City("Islamshahr", 35.5656, 51.2369),
            City("Pakdasht", 35.4794, 51.6978),
            City("Varamin", 35.3242, 51.6481),
            City("Damavand", 35.7075, 52.0542),
            City("Karaj", 35.8327, 50.9915),
        ]
    ),
    Province(
        name="Alborz",
        name_persian="البرز",
        code=2,
        latitude=36.0667,
        longitude=50.8667,
        cities=[
            City("Karaj", 35.8327, 50.9915),
            City("Hashtgerd", 35.9586, 50.9556),
            City("Nazabad", 35.9289, 50.7367),
            City("Savojbolagh", 35.9697, 50.6772),
            City("Taleqan", 36.1914, 50.6597),
            City("Eshtehard", 35.6967, 50.3222),
        ]
    ),
    Province(
        name="Isfahan",
        name_persian="اصفهان",
        code=3,
        latitude=32.6546,
        longitude=51.6678,
        cities=[
            City("Isfahan", 32.6546, 51.6678),
            City("Kashan", 33.9831, 51.4364),
            City("Najafabad", 32.6381, 51.3689),
            City("Shahreza", 32.0094, 51.8581),
            City("Mobarak-e Sar", 32.7344, 51.2369),
            City("Fuladshahr", 32.5303, 51.3728),
            City("Khomeyni Shahr", 32.6956, 51.5236),
            City("Natanz", 33.5169, 51.7331),
            City("Ardestan", 33.3739, 52.3753),
        ]
    ),
    Province(
        name="Fars",
        name_persian="فارس",
        code=4,
        latitude=29.5918,
        longitude=52.5837,
        cities=[
            City("Shiraz", 29.5918, 52.5837),
            City("Marvdasht", 29.8642, 52.8067),
            City("Fasa", 28.9694, 53.6883),
            City("Jahrom", 28.5003, 53.5881),
            City("Kazerun", 29.6158, 51.6544),
            City("Lar", 27.6753, 54.3325),
            City("Neyriz", 29.1983, 54.3275),
            City("Estahban", 29.1292, 54.0294),
            City("Zarqan", 29.7583, 52.7208),
        ]
    ),
    Province(
        name="Khorasan Razavi",
        name_persian="خراسان رضوی",
        code=5,
        latitude=36.3172,
        longitude=59.5628,
        cities=[
            City("Mashhad", 36.3172, 59.5628),
            City("Neishabur", 36.2089, 58.6831),
            City("Sabzevar", 36.2186, 57.6833),
            City("Torbat-e Heydarieh", 35.2742, 59.2250),
            City("Torbat-e Jam", 35.2417, 60.6214),
            City("Quchan", 37.1089, 58.5039),
            City("Kashmar", 35.2428, 58.4647),
            City("Nishapur", 36.2089, 58.6831),
            City("Gonabad", 34.5656, 58.6819),
        ]
    ),
    Province(
        name="East Azerbaijan",
        name_persian="آذربایجان شرقی",
        code=6,
        latitude=38.0818,
        longitude=46.2889,
        cities=[
            City("Tabriz", 38.0818, 46.2889),
            City("Maragheh", 37.3839, 46.2525),
            City("Ahar", 38.4758, 47.0689),
            City("Mianeh", 37.4531, 47.6861),
            City("Bostanabad", 37.8408, 46.8675),
            City("Azarshahr", 37.8567, 45.9256),
            City("Bonab", 37.4758, 46.0667),
            City("Sardasht", 38.3667, 46.1667),
        ]
    ),
    Province(
        name="West Azerbaijan",
        name_persian="آذربایجان غربی",
        code=7,
        latitude=37.5531,
        longitude=45.0764,
        cities=[
            City("Urmia", 37.5531, 45.0764),
            City("Khoy", 38.5503, 44.9519),
            City("Mahabad", 36.7675, 45.7239),
            City("Maku", 39.1975, 44.5150),
            City("Salmas", 38.2011, 44.7683),
            City("Mehran", 37.2667, 45.2000),
            City("Takab", 36.4000, 47.1167),
            City("Shahindej", 37.0953, 45.5461),
        ]
    ),
    Province(
        name="Kermanshah",
        name_persian="کرمانشاه",
        code=8,
        latitude=34.3142,
        longitude=47.0650,
        cities=[
            City("Kermanshah", 34.3142, 47.0650),
            City("Sahneh", 34.5011, 47.4469),
            City("Harsin", 34.3358, 47.5883),
            City("Kangavar", 34.5050, 47.9869),
            City("Qasr-e Shirin", 34.1303, 45.5747),
            City("Sarpol-e Zahab", 34.4669, 45.8567),
            City("Eslamabad-e Gharb", 34.1067, 46.4639),
            City("Paveh", 35.0619, 46.1586),
        ]
    ),
    Province(
        name="Khuzestan",
        name_persian="خوزستان",
        code=9,
        latitude=31.3188,
        longitude=48.6706,
        cities=[
            City("Ahvaz", 31.3188, 48.6706),
            City("Abadan", 30.3450, 48.2667),
            City("Khorramshahr", 30.4347, 48.1656),
            City("Shushtar", 32.0519, 48.8517),
            City("Dezful", 32.3764, 48.3939),
            City("Behbahan", 30.5967, 50.2417),
            City("Mahshahr", 30.5450, 49.1828),
            City("Andimeshk", 32.4578, 48.3519),
            City("Bandar-e Mahshahr", 30.5450, 49.1828),
        ]
    ),
    Province(
        name="Kerman",
        name_persian="کرمان",
        code=10,
        latitude=30.2839,
        longitude=57.0834,
        cities=[
            City("Kerman", 30.2839, 57.0834),
            City("Bam", 29.1089, 58.3539),
            City("Rafsanjan", 30.4058, 55.9953),
            City("Sirjan", 29.4569, 55.6836),
            City("Zarand", 31.1286, 56.5700),
            City("Bardsir", 29.9222, 57.0333),
            City("Baft", 29.2775, 56.5789),
            City("Jiroft", 28.6736, 57.7342),
            City("Kahnuj", 27.9542, 57.7242),
        ]
    ),
    Province(
        name="Yazd",
        name_persian="یزد",
        code=11,
        latitude=31.8974,
        longitude=54.3675,
        cities=[
            City("Yazd", 31.8974, 54.3675),
            City("Mehriz", 31.5519, 54.4097),
            City("Ardakan", 32.2858, 53.9647),
            City("Maybod", 32.2206, 54.0289),
            City("Taft", 31.7367, 54.2139),
            City("Bafq", 31.6028, 55.4014),
            City("Abarkuh", 31.1294, 53.2758),
            City("Saduq", 31.9414, 53.9639),
        ]
    ),
    Province(
        name="Hormozgan",
        name_persian="هرمزگان",
        code=12,
        latitude=27.1865,
        longitude=56.2789,
        cities=[
            City("Bandar Abbas", 27.1865, 56.2789),
            City("Qeshm", 26.9561, 56.2711),
            City("Minab", 27.0453, 57.0789),
            City("Bandar Lengeh", 26.5497, 54.8861),
            City("Rudan", 27.4039, 57.0081),
            City("Bastak", 27.2033, 54.8864),
            City("Hajiabad", 28.0064, 55.8317),
            City("Kish Island", 26.5378, 53.9817),
        ]
    ),
    Province(
        name="Hamadan",
        name_persian="همدان",
        code=13,
        latitude=34.7998,
        longitude=48.5147,
        cities=[
            City("Hamadan", 34.7998, 48.5147),
            City("Malayer", 34.2931, 48.8214),
            City("Nahavand", 34.1903, 48.3925),
            City("Tuyserkan", 34.5483, 48.4669),
            City("Asadabad", 34.7681, 48.1142),
            City("Bahar", 34.8878, 48.3544),
            City("Razan", 35.1686, 49.0267),
            City("Kabudarahang", 35.2117, 48.7447),
        ]
    ),
    Province(
        name="Gilan",
        name_persian="گیلان",
        code=14,
        latitude=37.2440,
        longitude=49.5661,
        cities=[
            City("Rasht", 37.2440, 49.5661),
            City("Lahijan", 37.1961, 50.0136),
            City("Bandar-e Anzali", 37.4658, 49.4589),
            City("Fuman", 37.2547, 49.3394),
            City("Astara", 38.4269, 48.8719),
            City("Rudbar", 36.8239, 49.4169),
            City("Talesh", 37.8333, 48.9167),
            City("Sowme'eh Sara", 37.3119, 49.2939),
        ]
    ),
    Province(
        name="Mazandaran",
        name_persian="مازندران",
        code=15,
        latitude=36.5358,
        longitude=52.3231,
        cities=[
            City("Sari", 36.5358, 52.3231),
            City("Babol", 36.5517, 52.6825),
            City("Amol", 36.4667, 52.3539),
            City("Qaem Shahr", 36.4758, 52.8644),
            City("Nowshahr", 36.6556, 51.5000),
            City("Chalus", 36.6514, 51.4208),
            City("Tonekabon", 36.8167, 50.8750),
            City("Behshahr", 36.6833, 53.5333),
            City("Savadkuh", 36.3167, 53.2333),
        ]
    ),
    Province(
        name="Golestan",
        name_persian="گلستان",
        code=16,
        latitude=36.8433,
        longitude=54.4392,
        cities=[
            City("Gorgan", 36.8433, 54.4392),
            City("Gonbad-e Kavus", 37.2583, 55.1653),
            City("Aliabad", 36.9214, 54.8700),
            City("Bandar-e Torkaman", 36.9056, 54.0694),
            City("Kordkuy", 36.8458, 54.0847),
            City("Bandar-e Gaz", 36.7347, 54.0306),
            City("Minudasht", 37.2378, 55.2894),
            City("Kalaleh", 37.3783, 55.4964),
        ]
    ),
    Province(
        name="Kohgiluyeh and Boyer-Ahmad",
        name_persian="کهگیلویه و بویراحمد",
        code=17,
        latitude=30.6508,
        longitude=51.5903,
        cities=[
            City("Yasuj", 30.6508, 51.5903),
            City("Dehdasht", 30.8269, 50.8614),
            City("Gachsaran", 30.3492, 50.7975),
            City("Likak", 30.6561, 50.5097),
            City("Bahmai", 30.9500, 50.6167),
            City("Dishmok", 30.6500, 50.3667),
        ]
    ),
    Province(
        name="Bushehr",
        name_persian="بوشهر",
        code=18,
        latitude=28.9234,
        longitude=50.8231,
        cities=[
            City("Bushehr", 28.9234, 50.8231),
            City("Bandar-e Ganaveh", 29.5756, 50.5239),
            City("Dashti", 29.4531, 50.4556),
            City("Dashtestan", 29.2839, 51.0611),
            City("Kangan", 27.8364, 51.9069),
            City("Bandar-e Dayyer", 28.7772, 51.2508),
            City("Tangestan", 29.1500, 51.1500),
        ]
    ),
    Province(
        name="Zanjan",
        name_persian="زنجان",
        code=19,
        latitude=36.6736,
        longitude=48.4839,
        cities=[
            City("Zanjan", 36.6736, 48.4839),
            City("Abhar", 36.1500, 49.2167),
            City("Khodabandeh", 36.1175, 48.5750),
            City("Qeydar", 36.9083, 48.7458),
            City("Mahneshan", 36.6389, 48.4900),
            City("Ijrud", 36.7333, 48.5667),
            City("Khorramdarreh", 36.2156, 49.1972),
            City("Tarom", 36.8764, 49.0531),
        ]
    ),
    Province(
        name="Semnan",
        name_persian="سمنان",
        code=20,
        latitude=35.5769,
        longitude=53.3931,
        cities=[
            City("Semnan", 35.5769, 53.3931),
            City("Damghan", 36.1533, 54.3439),
            City("Shahrud", 36.4156, 55.0181),
            City("Garmsar", 35.2175, 52.3136),
            City("Mehdishahr", 35.7169, 53.3822),
            City("Sorkheh", 35.4644, 53.7122),
            City("Biarjomand", 36.4833, 55.4167),
        ]
    ),
    Province(
        name="Sistan and Baluchestan",
        name_persian="سیستان و بلوچستان",
        code=21,
        latitude=29.4963,
        longitude=60.8629,
        cities=[
            City("Zahedan", 29.4963, 60.8629),
            City("Zabol", 31.0289, 61.4914),
            City("Chabahar", 25.2867, 60.6217),
            City("Iranshahr", 27.2017, 60.6839),
            City("Saravan", 27.3642, 62.3389),
            City("Khash", 28.2208, 61.2133),
            City("Nikshahr", 26.2389, 60.2167),
            City("Sarbaz", 26.4333, 61.8333),
        ]
    ),
    Province(
        name="Kurdistan",
        name_persian="کردستان",
        code=22,
        latitude=35.3156,
        longitude=46.9964,
        cities=[
            City("Sanandaj", 35.3156, 46.9964),
            City("Marivan", 35.5278, 46.1781),
            City("Saghez", 36.2536, 46.2611),
            City("Baneh", 35.9911, 45.8778),
            City("Qorveh", 35.0797, 47.8056),
            City("Divandarreh", 36.0919, 46.9381),
            City("Kamyaran", 34.9406, 47.0950),
            City("Bijar", 35.8694, 47.5953),
        ]
    ),
    Province(
        name="Markazi",
        name_persian="مرکزی",
        code=23,
        latitude=34.0996,
        longitude=49.6998,
        cities=[
            City("Arak", 34.0996, 49.6998),
            City("Saveh", 35.0639, 50.3472),
            City("Khomein", 33.9947, 50.0647),
            City("Mahallat", 33.8750, 50.4986),
            City("Delijan", 33.9897, 50.6842),
            City("Tafresh", 34.6944, 50.0111),
            City("Ashtian", 34.5169, 50.0111),
            City("Shazand", 33.9333, 49.4167),
        ]
    ),
    Province(
        name="Chaharmahal and Bakhtiari",
        name_persian="چهارمحال و بختیاری",
        code=24,
        latitude=32.3252,
        longitude=50.8625,
        cities=[
            City("Shahrekord", 32.3252, 50.8625),
            City("Borujen", 32.2686, 51.2942),
            City("Farsan", 32.2389, 50.5858),
            City("Lordegan", 31.4981, 51.2247),
            City("Ardal", 32.2739, 50.5456),
            City("Kiar", 31.9547, 50.8483),
            City("Samun", 32.0667, 50.7667),
            City("Farrokhshahr", 32.3961, 50.9389),
        ]
    ),
    Province(
        name="Qazvin",
        name_persian="قزوین",
        code=25,
        latitude=36.2675,
        longitude=50.0044,
        cities=[
            City("Qazvin", 36.2675, 50.0044),
            City("Takestan", 36.0697, 49.6983),
            City("Buin Zahra", 35.7678, 50.0508),
            City("Abyek", 36.0689, 50.5536),
            City("Alamut-e Sharqi", 36.4333, 50.5500),
            City("Alamut-e Gharbi", 36.4500, 50.5667),
            City("Avaj", 35.5853, 49.3828),
        ]
    ),
    Province(
        name="Ilam",
        name_persian="ایلام",
        code=26,
        latitude=33.6358,
        longitude=46.4236,
        cities=[
            City("Ilam", 33.6358, 46.4236),
            City("Eivan", 33.7417, 46.0500),
            City("Dehloran", 33.2858, 46.9128),
            City("Darreh Shahr", 33.1306, 47.4242),
            City("Mehran", 33.1278, 46.1781),
            City("Abdanan", 33.1169, 47.4142),
            City("Shirvan", 33.6508, 46.3333),
            City("Badreh", 33.1500, 47.2000),
        ]
    ),
    Province(
        name="Lorestan",
        name_persian="لرستان",
        code=27,
        latitude=33.4956,
        longitude=48.3533,
        cities=[
            City("Khorramabad", 33.4956, 48.3533),
            City("Borujerd", 33.8969, 48.7539),
            City("Dorud", 33.4967, 49.2219),
            City("Kuhdasht", 33.5256, 47.6197),
            City("Aligudarz", 33.4008, 49.6881),
            City("Nurabad", 34.0764, 47.9739),
            City("Azna", 33.8756, 49.4633),
            City("Pol-e Dokhtar", 33.5117, 47.7289),
        ]
    ),
    Province(
        name="South Khorasan",
        name_persian="خراسان جنوبی",
        code=28,
        latitude=32.8669,
        longitude=59.2169,
        cities=[
            City("Birjand", 32.8669, 59.2169),
            City("Bojnurd", 37.4728, 57.3267),
            City("Ghayen", 33.7378, 59.1786),
            City("Ferdows", 34.0147, 58.1675),
            City("Tabas", 33.5964, 56.9361),
            City("Sarayan", 32.9364, 58.9597),
            City("Zirkuh", 32.8667, 59.7667),
        ]
    ),
    Province(
        name="North Khorasan",
        name_persian="خراسان شمالی",
        code=29,
        latitude=37.4728,
        longitude=57.3267,
        cities=[
            City("Bojnurd", 37.4728, 57.3267),
            City("Shirvan", 37.4247, 57.9364),
            City("Esfarayen", 37.0800, 57.5069),
            City("Garmeh", 36.9739, 57.2311),
            City("Jajrom", 37.3614, 57.7819),
            City("Maneh", 37.4000, 57.5000),
            City("Sankhast", 36.9500, 57.5000),
        ]
    ),
    Province(
        name="Ardabil",
        name_persian="اردبیل",
        code=30,
        latitude=38.2506,
        longitude=48.2935,
        cities=[
            City("Ardabil", 38.2506, 48.2935),
            City("Parsabad", 39.6481, 47.9158),
            City("Meshginshahr", 38.3956, 47.9964),
            City("Namin", 38.4167, 48.4667),
            City("Khalkhal", 37.6217, 48.5267),
            City("Sarein", 38.1500, 48.0833),
            City("Germi", 38.9697, 47.9206),
        ]
    ),
    Province(
        name="Qom",
        name_persian="قم",
        code=31,
        latitude=34.6409,
        longitude=50.8764,
        cities=[
            City("Qom", 34.6409, 50.8764),
            City("Kahak", 34.7667, 50.8833),
            City("Qahan", 34.5167, 50.8667),
        ]
    ),
]


def get_province_by_name(name: str) -> Optional[Province]:
    """Get province by name (case-insensitive)."""
    for province in IRANIAN_PROVINCES:
        if province.name.lower() == name.lower():
            return province
    return None


def get_province_by_code(code: int) -> Optional[Province]:
    """Get province by code."""
    for province in IRANIAN_PROVINCES:
        if province.code == code:
            return province
    return None


def get_all_province_names() -> List[str]:
    """Get list of all province names."""
    return [p.name for p in IRANIAN_PROVINCES]


def get_cities_in_province(province_name: str) -> List[str]:
    """Get list of cities in a province."""
    province = get_province_by_name(province_name)
    if province:
        return [city.name for city in province.cities]
    return []


def get_city_coordinates(city_name: str, province_name: str = None) -> Optional[tuple]:
    """Get coordinates for a city (optionally within a province)."""
    for province in IRANIAN_PROVINCES:
        if province_name and province.name.lower() != province_name.lower():
            continue
        
        city = province.get_city(city_name)
        if city:
            return (city.latitude, city.longitude)
    return None


def get_province_by_coordinates(latitude: float, longitude: float, 
                                  tolerance: float = 0.5) -> Optional[Province]:
    """
    Find province by coordinates within tolerance (degrees).
    This is approximate; for production, use proper point-in-polygon algorithms.
    """
    best_match = None
    min_distance = float('inf')
    
    for province in IRANIAN_PROVINCES:
        # Calculate distance using Haversine formula (simplified to Euclidean for local)
        lat_diff = abs(latitude - province.latitude)
        lon_diff = abs(longitude - province.longitude)
        distance = (lat_diff ** 2 + lon_diff ** 2) ** 0.5
        
        if distance < tolerance and distance < min_distance:
            min_distance = distance
            best_match = province
    
    return best_match
