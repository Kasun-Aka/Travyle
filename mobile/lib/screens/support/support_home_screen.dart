import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import 'support_center_screen.dart';
import '../vouchers/voucher_wallet_screen.dart';
import '../reviews/tour_review_screen.dart';

class SupportHomeScreen extends StatelessWidget {
  const SupportHomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Travyle Support & Care', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.white)),
        backgroundColor: AppTheme.primaryDark,
        elevation: 0,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Welcome Banner
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [AppTheme.primaryDark, Color(0xFF1E6F86)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: const [
                      Icon(Icons.verified_user_outlined, color: AppTheme.coral, size: 24),
                      SizedBox(width: 8),
                      Text(
                        '24/7 TRAVELER QUALITY CARE',
                        style: TextStyle(color: Colors.white70, fontSize: 11, fontWeight: FontWeight.bold, letterSpacing: 1.2),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  const Text(
                    'Need Help or Experiencing Disruption?',
                    style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 6),
                  const Text(
                    'Our AI Agent triages claims instantly and generates goodwill compensation for verified disruptions.',
                    style: TextStyle(color: Colors.white70, fontSize: 13, height: 1.4),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            const Text(
              'Support Services',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
            ),
            const SizedBox(height: 14),

            // Card 1: Support Center
            _buildActionCard(
              context: context,
              icon: Icons.support_agent,
              iconColor: const Color(0xFF4F46E5),
              iconBg: const Color(0xFFEEF2FF),
              title: 'Traveler Support Center',
              subtitle: 'Submit claims with camera photo attachments & track AI triage status',
              badge: 'AI Automated',
              badgeColor: const Color(0xFF7C3AED),
              onTap: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const SupportCenterScreen()),
              ),
            ),
            const SizedBox(height: 14),

            // Card 2: Digital Voucher Wallet
            _buildActionCard(
              context: context,
              icon: Icons.card_giftcard,
              iconColor: const Color(0xFF16A34A),
              iconBg: const Color(0xFFDCFCE7),
              title: 'Digital Voucher Wallet',
              subtitle: 'Access approved \$50 goodwill vouchers, promo passes & copy codes',
              badge: 'Wallet Pass',
              badgeColor: const Color(0xFF16A34A),
              onTap: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const VoucherWalletScreen()),
              ),
            ),
            const SizedBox(height: 14),

            // Card 3: Tour Reviews & Feedback
            _buildActionCard(
              context: context,
              icon: Icons.star_rate_rounded,
              iconColor: const Color(0xFFD97706),
              iconBg: const Color(0xFFFEF3C7),
              title: 'Tour Ratings & Reviews',
              subtitle: 'Share verified 5-star ratings and guide feedback for your completed tours',
              badge: 'Verified',
              badgeColor: const Color(0xFFD97706),
              onTap: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const TourReviewScreen()),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActionCard({
    required BuildContext context,
    required IconData icon,
    required Color iconColor,
    required Color iconBg,
    required String title,
    required String subtitle,
    required String badge,
    required Color badgeColor,
    required VoidCallback onTap,
  }) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      child: InkWell(
        borderRadius: BorderRadius.circular(14),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(18),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: iconBg,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, color: iconColor, size: 28),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Flexible(
                          child: Text(
                            title,
                            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(
                            color: badgeColor.withAlpha(30),
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: Text(
                            badge,
                            style: TextStyle(color: badgeColor, fontSize: 10, fontWeight: FontWeight.bold),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      subtitle,
                      style: const TextStyle(fontSize: 13, color: Color(0xFF64748B), height: 1.4),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 6),
              const Icon(Icons.arrow_forward_ios, size: 14, color: AppTheme.textLight),
            ],
          ),
        ),
      ),
    );
  }
}
