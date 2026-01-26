import axiosClient from './axiosClient';

export interface DocumentUploadDto {
    title: string;
    documentType: string;
    subjectId?: number;
    semester?: string;
    year?: number;
    isPublic: boolean;
    isActive?: boolean;
    file: File;
}

export interface DocumentTextDto {
    title?: string;
    content: string;
    documentType: string;
    isPublic: boolean;
    isActive?: boolean;
}

export interface DocumentDto {
    id: number;
    title: string;
    documentType: string;
    originalFileName: string;
    fileSize: number;
    userId: number;
    userName: string;
    subjectId?: number;
    subjectName?: string;
    semester?: string;
    year?: number;
    uploadDate: string;
    isPublic: boolean;
    isActive: boolean;
}

const documentApi = {
    // Lấy danh sách tất cả tài liệu
    getAll: (params?: any): Promise<DocumentDto[]> => {
        return axiosClient.get('/documents', { params });
    },
    // Lấy chi tiết tài liệu theo ID
    getById: (id: number): Promise<DocumentDto> => {
        return axiosClient.get(`/documents/${id}`);
    },
    // Tải lên tài liệu mới dưới dạng tệp
    upload: (data: DocumentUploadDto): Promise<DocumentDto> => {
        const formData = new FormData();
        formData.append('file', data.file);
        formData.append('title', data.title);
        formData.append('documentType', data.documentType);
        if (data.subjectId) formData.append('subjectId', data.subjectId.toString());
        if (data.semester) formData.append('semester', data.semester);
        if (data.year) formData.append('year', data.year.toString());
        formData.append('isPublic', data.isPublic.toString());
        if (data.isActive !== undefined) formData.append('isActive', data.isActive.toString());

        return axiosClient.post('/documents/upload', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
    },
    // Tạo tài liệu mới từ văn bản dán
    createFromText: (data: DocumentTextDto): Promise<DocumentDto> => {
        return axiosClient.post('/documents/paste-text', data);
    },
    // Cập nhật thông tin tài liệu
    update: (id: number, data: any): Promise<DocumentDto> => {
        return axiosClient.put(`/documents/${id}`, data);
    },
    // Xóa tài liệu (Xóa mềm)
    delete: (id: number): Promise<void> => {
        return axiosClient.delete(`/documents/${id}`);
    },
    // Lấy nội dung văn bản thuần của tài liệu
    getContent: (id: number): Promise<{ content: string }> => {
        return axiosClient.get(`/documents/${id}/content`);
    },
    // Lấy đường dẫn tải xuống tệp gốc
    getDownloadUrl: (id: number): string => {
        const baseUrl = import.meta.env.VITE_API_URL || '/api';
        const token = localStorage.getItem('token');
        return `${baseUrl}/documents/${id}/download?token=${token}`;
    }
};

export default documentApi;
