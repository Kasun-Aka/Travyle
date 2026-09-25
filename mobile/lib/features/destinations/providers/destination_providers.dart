import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../models/destination.dart';
import '../services/destination_api_service.dart';

final destinationsProvider = FutureProvider.autoDispose<List<Destination>>((
  ref,
) async {
  final items = await destinationApiService.getDestinations();
  return items.map(Destination.fromJson).toList();
});
