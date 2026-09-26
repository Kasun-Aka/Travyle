import re

with open("mobile/lib/screens/home_screen.dart", "r", encoding="utf-8") as f:
    text = f.read()

old_block = r"""          // AI Recommended Cards
          if \(_adminRecommendations\.isEmpty\) \.\.\.\[
            _buildRecommendedCard\(
              context,
              'Mediterranean Wellness Retreat',
              'Sardinia',
              '\$4,250',
              '8 Days',
              'https://images\.unsplash\.com/photo-1510414842594-a61c69b5ae57\?w=800&h=400&fit=crop',
            \),
            const SizedBox\(height: 24\),
            _buildRecommendedCard\(
              context,
              'Cultural & Culinary Silk Road',
              'Samarkand',
              '\$6,100',
              '12 Days',
              'https://images\.unsplash\.com/photo-1596700854497-874b3d81b942\?w=800&h=400&fit=crop',
            \),
          \] else \.\.\.\[
            ListView\.builder\(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics\(\),
              itemCount: _adminRecommendations\.length,
              itemBuilder: \(context, index\) \{
                final notif = _adminRecommendations\[index\];
                final dest = notif\["destination"\] \?\? \{\};
                return Padding\(
                  padding: const EdgeInsets\.only\(bottom: 24\.0\),
                  child: _buildAdminRecommendedCard\(context, notif, dest\),
                \);
              \},
            \),
          \],"""

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
          ),"""

text = re.sub(old_block, new_block, text, flags=re.DOTALL)

with open("mobile/lib/screens/home_screen.dart", "w", encoding="utf-8") as f:
    f.write(text)

print("Success")
