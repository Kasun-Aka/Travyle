with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

import re

# We will remove the static cards.
# Wait, I added a Divider and then the two static cards previously. Let's find that block and remove it.
old_block = r"""          if \(_adminRecommendations\.isNotEmpty\)
            const Padding\(
              padding: EdgeInsets\.symmetric\(vertical: 8\.0\),
              child: Divider\(color: AppTheme\.borderLight\),
            \),
            
          _buildRecommendedCard\(
            context,
            'Mediterranean Wellness Retreat',
            'Sardinia',
            '\\\$4,250',
            '8 Days',
            'https://images\.unsplash\.com/photo-1510414842594-a61c69b5ae57\?w=800&h=400&fit=crop',
          \),
          const SizedBox\(height: 24\),
          _buildRecommendedCard\(
            context,
            'Cultural & Culinary Silk Road',
            'Samarkand',
            '\\\$6,100',
            '12 Days',
            'https://images\.unsplash\.com/photo-1542640244-76abc29e4368\?w=800&h=400&fit=crop',
          \),"""

text = re.sub(old_block, "", text)

with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Removed dummy cards!")
