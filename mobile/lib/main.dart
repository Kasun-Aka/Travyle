import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'screens/splash_screen.dart';
import 'screens/login_screen.dart';
import 'screens/welcome_screen.dart';
import 'theme/app_theme.dart';
import 'features/bookings/screens/booking_history_screen.dart';
import 'features/bookings/screens/discount_request_history_screen.dart';
import 'features/bookings/screens/schedule_browse_screen.dart';
import 'features/bookings/theme/booking_theme.dart';
import 'features/bookings/widgets/primary_gradient_button.dart';

void main() {
  runApp(const ProviderScope(child: TravyleApp()));
}

class TravyleApp extends StatelessWidget {
  const TravyleApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Travyle',
      debugShowCheckedModeBanner: false,
      theme: BookingTheme.themeData,
      home: const SplashScreen(),
      routes: {
        '/splash': (context) => const SplashScreen(),
        '/login': (context) => const LoginScreen(),
        '/welcome': (context) => const WelcomeScreen(),
        '/schedules': (context) => const ScheduleBrowseScreen(),
        '/history': (context) => const BookingHistoryScreen(),
        '/discount-requests': (context) => const DiscountRequestHistoryScreen(),
        '/prototype': (context) => const BookingScreen(),
      },
    );
  }
}

typedef MyApp = TravyleApp;

class BookingScreen extends StatefulWidget {
  const BookingScreen({super.key});

  @override
  State<BookingScreen> createState() => _BookingScreenState();
}

class _BookingScreenState extends State<BookingScreen> {
  int guests = 2;
  String selectedDate = 'Tue, 29 Jul';
  String selectedSlot = '08:00 AM';

  final List<String> dates = const [
    'Tue, 29 Jul',
    'Wed, 30 Jul',
    'Thu, 31 Jul',
    'Fri, 1 Aug',
  ];

  final List<String> slots = const [
    '08:00 AM',
    '10:30 AM',
    '01:00 PM',
    '04:30 PM',
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F3F0),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 20, 20, 120),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(14),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: 0.05),
                          blurRadius: 12,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: const Icon(Icons.arrow_back_ios_new_rounded, size: 18),
                  ),
                  const Spacer(),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: const Text(
                      'Secure booking',
                      style: TextStyle(
                        fontWeight: FontWeight.w600,
                        color: Color(0xFF1A3A4A),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),
              const Text(
                'Book your escape',
                style: TextStyle(
                  fontSize: 30,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF0F2B38),
                ),
              ),
              const SizedBox(height: 18),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(18),
                decoration: BoxDecoration(
                  gradient: const LinearGradient(
                    colors: [Color(0xFF1A3A4A), Color(0xFF2B5A6A)],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                  borderRadius: BorderRadius.circular(26),
                  boxShadow: [
                    BoxShadow(
                      color: const Color(0xFFD4764E).withValues(alpha: 0.25),
                      blurRadius: 20,
                      offset: const Offset(0, 12),
                    ),
                  ],
                ),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Ella Adventure',
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w800,
                              color: Colors.white,
                            ),
                          ),
                          const SizedBox(height: 8),
                          Row(
                            children: const [
                              Icon(Icons.location_on_outlined, color: Colors.white70, size: 16),
                              SizedBox(width: 6),
                              Text(
                                'Nuwara Eliya, Sri Lanka',
                                style: TextStyle(
                                  color: Colors.white70,
                                  fontSize: 13,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 14),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: const Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(Icons.star_rounded, size: 14, color: Color(0xFFFFD166)),
                                SizedBox(width: 4),
                                Text(
                                  '4.9 • 245 reviews',
                                  style: TextStyle(
                                    color: Colors.white,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    Container(
                      width: 82,
                      height: 82,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.18),
                        borderRadius: BorderRadius.circular(22),
                      ),
                      child: const Icon(
                        Icons.tour_rounded,
                        size: 36,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),
              _SectionTitle(title: 'Select date'),
              const SizedBox(height: 12),
              SizedBox(
                height: 52,
                child: ListView.separated(
                  scrollDirection: Axis.horizontal,
                  itemCount: dates.length,
                  separatorBuilder: (_, _) => const SizedBox(width: 10),
                  itemBuilder: (context, index) {
                    final isSelected = dates[index] == selectedDate;
                    return ChoiceChip(
                      label: Text(dates[index]),
                      selected: isSelected,
                      onSelected: (_) => setState(() => selectedDate = dates[index]),
                    );
                  },
                ),
              ),
              const SizedBox(height: 24),
              _SectionTitle(title: 'Select slot'),
              const SizedBox(height: 12),
              Wrap(
                spacing: 10,
                runSpacing: 10,
                children: slots.map((slot) {
                  final isSelected = slot == selectedSlot;
                  return FilterChip(
                    label: Text(slot),
                    selected: isSelected,
                    onSelected: (_) => setState(() => selectedSlot = slot),
                  );
                }).toList(),
              ),
              const SizedBox(height: 24),
              _SectionTitle(title: 'Guests'),
              const SizedBox(height: 12),
              Row(
                children: [
                  IconButton(
                    onPressed: () => setState(() => guests = (guests > 1 ? guests - 1 : 1)),
                    icon: const Icon(Icons.remove_circle_outline),
                  ),
                  Text(
                    '$guests',
                    style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w700),
                  ),
                  IconButton(
                    onPressed: () => setState(() => guests = guests + 1),
                    icon: const Icon(Icons.add_circle_outline),
                  ),
                ],
              ),
              const SizedBox(height: 30),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () {},
                  style: ElevatedButton.styleFrom(
                    backgroundColor: const Color(0xFF1A3A4A),
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                    ),
                  ),
                  child: const Text('Continue to payment'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String title;
  const _SectionTitle({required this.title});

  @override
  Widget build(BuildContext context) {
    return Text(
      title,
      style: const TextStyle(
        color: Color(0xFF1A3A4A),
        fontWeight: FontWeight.w700,
        fontSize: 16,
      ),
    );
  }
}