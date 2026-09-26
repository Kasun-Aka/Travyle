with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

import re

# We can just match from "// AI Recommended Cards" to "const SizedBox(height: 40),"
start_str = '          // AI Recommended Cards\n'
end_str = '          const SizedBox(height: 40),\n        ],\n      ),\n    );'

if start_str in text and end_str in text:
    start_idx = text.find(start_str)
    end_idx = text.find(end_str)
    
    new_block = """          // AI Recommended Cards
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
          
          if (_adminRecommendations.isNotEmpty)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 8.0),
              child: Divider(color: AppTheme.borderLight),
            ),
            
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
            'https://images.unsplash.com/photo-1542640244-76abc29e4368?w=800&h=400&fit=crop',
          ),
"""
    
    text = text[:start_idx] + new_block + text[end_idx:]
    with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
        f.write(text)
    print("Replaced!")
else:
    print("Could not find boundaries")
