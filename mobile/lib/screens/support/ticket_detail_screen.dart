import 'package:flutter/material.dart';
import '../../models/support_ticket.dart';
import '../../services/support_api_service.dart';
import '../../theme/app_theme.dart';

class TicketDetailScreen extends StatefulWidget {
  final String ticketId;

  const TicketDetailScreen({super.key, required this.ticketId});

  @override
  State<TicketDetailScreen> createState() => _TicketDetailScreenState();
}

class _TicketDetailScreenState extends State<TicketDetailScreen> {
  final SupportApiService _apiService = SupportApiService();
  SupportTicketModel? _ticket;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadTicket();
  }

  Future<void> _loadTicket() async {
    setState(() => _isLoading = true);
    final data = await _apiService.getTicketById(widget.ticketId);
    if (!mounted) return;
    setState(() {
      _ticket = data;
      _isLoading = false;
    });
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return Colors.amber.shade800;
      case 'Pending_AI_Triage':
        return Colors.indigo;
      case 'Resolved':
        return Colors.green.shade700;
      case 'In_Review':
        return Colors.blue.shade700;
      default:
        return Colors.grey.shade700;
    }
  }

  String _formatStatus(String status) {
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return '⏳ Awaiting Admin Voucher Approval';
      case 'Pending_AI_Triage':
        return '🤖 AI Triaging Claim';
      case 'In_Review':
        return '🔍 In Review';
      case 'Resolved':
        return '✓ Claim Resolved';
      default:
        return status;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Claim Details & Trace', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: AppTheme.primaryDark,
        iconTheme: const IconThemeData(color: Colors.white),
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _ticket == null
              ? const Center(child: Text('Ticket not found'))
              : RefreshIndicator(
                  onRefresh: _loadTicket,
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Status Header Card
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: _getStatusColor(_ticket!.status).withAlpha(20),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: _getStatusColor(_ticket!.status).withAlpha(75)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text(
                                    _formatStatus(_ticket!.status),
                                    style: TextStyle(
                                      color: _getStatusColor(_ticket!.status),
                                      fontWeight: FontWeight.bold,
                                      fontSize: 14,
                                    ),
                                  ),
                                  Text(
                                    'Priority: ${_ticket!.priority}',
                                    style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 12),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Text(
                                _ticket!.title,
                                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppTheme.primaryDark),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),

                        // Description & Photo
                        const Text('Your Statement', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.textDark)),
                        const SizedBox(height: 8),
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(color: const Color(0xFFE2E8F0)),
                          ),
                          child: Text(_ticket!.description, style: const TextStyle(fontSize: 14, height: 1.5)),
                        ),
                        if (_ticket!.attachmentUrl != null) ...[
                          const SizedBox(height: 14),
                          const Text('Attached Photo Evidence', style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600)),
                          const SizedBox(height: 8),
                          ClipRRect(
                            borderRadius: BorderRadius.circular(12),
                            child: Image.network(
                              _ticket!.attachmentUrl!,
                              height: 180,
                              width: double.infinity,
                              fit: BoxFit.cover,
                              errorBuilder: (context, error, stackTrace) => const Icon(Icons.broken_image, size: 60),
                            ),
                          ),
                        ],
                        const SizedBox(height: 24),

                        // AI Triage & Goodwill Voucher Card
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(18),
                          decoration: BoxDecoration(
                            gradient: const LinearGradient(
                              colors: [Color(0xFFF5F3FF), Color(0xFFEDE9FE)],
                              begin: Alignment.topLeft,
                              end: Alignment.bottomRight,
                            ),
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: const Color(0xFFDDD6FE)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: const [
                                  Icon(Icons.smart_toy_outlined, color: Color(0xFF7C3AED)),
                                  SizedBox(width: 8),
                                  Text(
                                    'AI Agent Assessment',
                                    style: TextStyle(fontWeight: FontWeight.bold, color: Color(0xFF6D28D9), fontSize: 15),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'Severity Tier: ${_ticket!.severityTier} | Sentiment: ${_ticket!.sentimentScore.toStringAsFixed(2)}',
                                style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13, color: Color(0xFF4C1D95)),
                              ),
                              if (_ticket!.aiReasoning != null) ...[
                                const SizedBox(height: 6),
                                Text(
                                  _ticket!.aiReasoning!,
                                  style: const TextStyle(fontSize: 13, color: Color(0xFF5B21B6), height: 1.4),
                                ),
                              ],
                            ],
                          ),
                        ),
                        const SizedBox(height: 24),

                        // Chronological Audit Trace
                        const Text('Resolution Timeline', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppTheme.textDark)),
                        const SizedBox(height: 12),
                        if (_ticket!.auditLogs.isEmpty)
                          const Text('No audit events yet.', style: TextStyle(color: AppTheme.textLight))
                        else
                          ListView.separated(
                            shrinkWrap: true,
                            physics: const NeverScrollableScrollPhysics(),
                            itemCount: _ticket!.auditLogs.length,
                            separatorBuilder: (context, index) => const SizedBox(height: 10),
                            itemBuilder: (context, index) {
                              final log = _ticket!.auditLogs[index];
                              return Container(
                                padding: const EdgeInsets.all(12),
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  borderRadius: BorderRadius.circular(10),
                                  border: Border.all(color: const Color(0xFFE2E8F0)),
                                ),
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Container(
                                      padding: const EdgeInsets.all(6),
                                      decoration: BoxDecoration(
                                        color: AppTheme.primaryDark.withAlpha(20),
                                        shape: BoxShape.circle,
                                      ),
                                      child: const Icon(Icons.check_circle_outline, size: 16, color: AppTheme.primaryDark),
                                    ),
                                    const SizedBox(width: 10),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Row(
                                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                            children: [
                                              Text(
                                                log.action,
                                                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13, color: AppTheme.textDark),
                                              ),
                                              Text(
                                                '${log.timestamp.hour}:${log.timestamp.minute.toString().padLeft(2, '0')}',
                                                style: const TextStyle(fontSize: 11, color: AppTheme.textLight),
                                              ),
                                            ],
                                          ),
                                          const SizedBox(height: 4),
                                          Text(log.details, style: const TextStyle(fontSize: 12, color: Color(0xFF475569))),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              );
                            },
                          ),
                      ],
                    ),
                  ),
                ),
    );
  }
}
