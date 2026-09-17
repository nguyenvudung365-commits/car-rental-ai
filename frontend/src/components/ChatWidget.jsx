import React, { useState, useEffect, useRef } from 'react';
import { FiMessageSquare, FiX, FiSend } from 'react-icons/fi';
import axiosClient from '../api/axiosClient';

/**
 * Trợ lý Chat AI nổi (ChatWidget)
 * Nút chat nổi cố định ở góc dưới bên phải màn hình.
 * Khi mở, người dùng có thể gửi câu hỏi và nhận câu trả lời từ endpoint POST /chat.
 */
const ChatWidget = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState([
    {
      role: 'ai',
      text: 'Xin chào! Tôi là trợ lý AI của Car Rental. Bạn cần tìm loại xe nào hay cần hỗ trợ gì không? 😊',
    },
  ]);
  const [inputText, setInputText] = useState('');
  const [loading, setLoading] = useState(false);

  const messagesEndRef = useRef(null);

  // Tự động cuộn xuống cuối khi có tin nhắn mới hoặc đang chờ câu trả lời
  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
    }
  }, [messages, loading, isOpen]);

  // Xử lý gửi tin nhắn tới API backend
  const handleSendMessage = async (e) => {
    if (e) e.preventDefault();

    const trimmed = inputText.trim();
    if (!trimmed || loading) return;

    // Thêm tin nhắn của người dùng vào giao diện
    const userMessage = { role: 'user', text: trimmed };
    setMessages((prev) => [...prev, userMessage]);
    setInputText('');
    setLoading(true);

    try {
      /**
       * Gửi yêu cầu tới API Chat:
       * POST /chat
       * Body: { message: inputText }
       * Phản hồi mong đợi: { reply: string, isError: boolean }
       */
      const response = await axiosClient.post('/chat', { message: trimmed });
      const data = response?.data !== undefined ? response.data : response;

      if (data?.isError) {
        setMessages((prev) => [
          ...prev,
          {
            role: 'ai',
            text: 'AI tạm thời không khả dụng, vui lòng thử lại sau 😊',
          },
        ]);
      } else {
        setMessages((prev) => [
          ...prev,
          {
            role: 'ai',
            text: data?.reply || 'Xin lỗi, tôi chưa nhận được câu trả lời cụ thể.',
          },
        ]);
      }
    } catch (error) {
      // Xử lý khi gặp sự cố mạng hoặc server không phản hồi
      console.error('Lỗi khi gọi API /chat:', error);
      setMessages((prev) => [
        ...prev,
        {
          role: 'ai',
          text: 'Không thể kết nối đến AI, vui lòng thử lại',
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <aside className="chat-widget-wrapper" aria-label="Hộp thoại trợ lý AI">
      {/* Cửa sổ chat mở rộng */}
      {isOpen && (
        <div className="chat-window">
          {/* Tiêu đề cửa sổ chat */}
          <div className="chat-header">
            <div className="chat-header-title">
              <span className="chat-header-icon">🤖</span>
              <span>Trợ lý AI Car Rental</span>
            </div>
            <button
              type="button"
              className="chat-close-btn"
              onClick={() => setIsOpen(false)}
              aria-label="Đóng hộp chat"
            >
              <FiX size={20} />
            </button>
          </div>

          {/* Danh sách tin nhắn */}
          <div className="chat-messages">
            {messages.map((msg, index) => {
              const isUser = msg.role === 'user';
              return (
                <div
                  key={index}
                  className={`chat-message-row ${isUser ? 'message-user' : 'message-ai'}`}
                >
                  <div className="message-bubble">
                    <p className="message-text">{msg.text}</p>
                  </div>
                </div>
              );
            })}

            {/* Hiển thị hiệu ứng tải khi AI đang suy nghĩ */}
            {loading && (
              <div className="chat-message-row message-ai">
                <div className="message-bubble loading-bubble">
                  <span className="typing-dot"></span>
                  <span className="typing-dot"></span>
                  <span className="typing-dot"></span>
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Form nhập nội dung tin nhắn */}
          <form className="chat-input-form" onSubmit={handleSendMessage}>
            <input
              type="text"
              className="chat-input"
              placeholder="Nhập tin nhắn..."
              value={inputText}
              onChange={(e) => setInputText(e.target.value)}
              disabled={loading}
            />
            <button
              type="submit"
              className="chat-send-btn"
              disabled={loading || !inputText.trim()}
              aria-label="Gửi tin nhắn"
            >
              <FiSend size={18} />
            </button>
          </form>
        </div>
      )}

      {/* Nút tròn nổi mở / đóng chat ở góc dưới bên phải */}
      <button
        type="button"
        className={`chat-bubble-toggle ${isOpen ? 'active' : ''}`}
        onClick={() => setIsOpen(!isOpen)}
        aria-label={isOpen ? 'Đóng chat' : 'Mở chat với AI'}
      >
        {isOpen ? <FiX size={26} /> : <FiMessageSquare size={26} />}
      </button>
    </aside>
  );
};

export default ChatWidget;
