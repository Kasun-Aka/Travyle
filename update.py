import re

with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

# Add _adminRecommendations to state
state_search = "  int _unreadNotifications = 0;"
state_replace = "  int _unreadNotifications = 0;\n  List<dynamic> _adminRecommendations = [];"
text = text.replace(state_search, state_replace)

# Update _fetchNotifications
fetch_search = """        if (mounted) {
          setState(() {
            _unreadNotifications = unread;
          });
        }"""
fetch_replace = """        if (mounted) {
          setState(() {
            _unreadNotifications = unread;
            _adminRecommendations = notifs;
          });
        }"""
text = text.replace(fetch_search, fetch_replace)

# Replace the hardcoded AI Recommended Cards
cards_regex = r"          // AI Recommended Cards.*?          const SizedBox\(height: 40\),"
cards_replace = """          // AI Recommended Cards
          if (_adminRecommendations.isEmpty) ...[
            _buildRecommendedCard(
              context,
              'Mediterranean Wellness Retreat',
              'Sardinia',
              '\\$4,250',
              '8 Days',
              'https://images.unsplash.com/photo-1510414842594-a61c69b5ae57?w=800&h=400&fit=crop',
            ),
            const SizedBox(height: 24),
            _buildRecommendedCard(
              context,
              'Cultural & Culinary Silk Road',
              'Samarkand',
              '\\$6,100',
              '12 Days',
              'https://images.unsplash.com/photo-1596700854497-874b3d81b942?w=800&h=400&fit=crop',
            ),
          ] else ...[
            ListView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: _adminRecommendations.length,
              itemBuilder: (context, index) {
                final notif = _adminRecommendations[index];
                final dest = notif["destination"] ?? {};
                return Padding(
                  padding: const EdgeInsets.only(bottom: 24.0),
                  child: _buildAdminRecommendedCard(context, notif, dest),
                );
              },
            ),
          ],
          const SizedBox(height: 40),"""

text = re.sub(cards_regex, cards_replace, text, flags=re.DOTALL)

new_method = """  Widget _buildAdminRecommendedCard(BuildContext context, dynamic notif, dynamic dest) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 15,
            offset: const Offset(0, 5),
          ),
        ],
        border: Border.all(
          color: (notif["isRead"] == true) ? Colors.transparent : AppTheme.primaryDark.withOpacity(0.3),
          width: 2,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ClipRRect(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
            child: Stack(
              children: [
                Image.network(
                  dest["imageUrl"] ?? 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800&h=400&fit=crop',
                  height: 160,
                  width: double.infinity,
                  fit: BoxFit.cover,
                ),
                if (notif["isRead"] == false)
                  Positioned(
                    top: 12,
                    left: 12,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: Colors.red,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Text(
                        'NEW MATCH',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        dest["name"] ?? 'Recommended Destination',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppTheme.textDark,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.location_on, size: 14, color: AppTheme.textGrey),
                    const SizedBox(width: 4),
                    Text(
                      dest["region"] ?? 'Global',
                      style: const TextStyle(color: AppTheme.textGrey, fontSize: 12),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: AppTheme.backgroundLight,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: AppTheme.borderLight),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Icon(Icons.auto_awesome, size: 16, color: AppTheme.accentCopper),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          '"${notif["pitch"] ?? ""}"',
                          style: const TextStyle(
                            fontSize: 12,
                            fontStyle: FontStyle.italic,
                            color: AppTheme.textGrey,
                            height: 1.4,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildDestinationCard("""

text = text.replace("  Widget _buildDestinationCard(", new_method)

with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Success")
